using System;
using Lytec.Common;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.ADSCL;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;
using static Lytec.Protocol.LyLCDTvBrt.ProgramInfo;
using Lytec.Protocol.Ly;

namespace Lytec.Protocol.LyLCDTvBrt;

public class Command
{

    public class Pack : Pack<Pack, CommandPack>
    {
        public const int MaxDataLength = 0x400;

        static Pack()
        {
            SendIdentifier = new byte[7] { (byte)'\x1b', (byte)'$', (byte)'T', (byte)'v', (byte)'B', (byte)'r', (byte)'t' };
            RecvIdentifier = new byte[7] { (byte)'\x1b', (byte)'&', (byte)'t', (byte)'V', (byte)'b', (byte)'R', (byte)'T' };
            MinDataLength = CommandPack.MinDataLength;
        }
    }

    [Endian(DefaultEndian)]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommandCode : uint
    {
        ReadAny = 0,
        WriteAny = 1,
        Reboot = 0x10,
    }

    public static bool Exec(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Pack? Answer, CommandPack command, Func<Pack, bool>? CheckIsSuccess = default, int extTimeout = 0)
    {
        var cmd = new Pack()
        {
            AddrCode = (byte)(conf.AddrCode ?? 0),
            Data = command.Clone(),
            Identifier = Pack.SendIdentifier.ToArray(),
        };
        cmd.UpdatePackIndex();
        cmd.UpdateCheckSum();
        var sbuf = cmd.Serialize();
        var deserializer = Pack.CreateDeserializer();
        Answer = null;
        for (var tryCount = -1; tryCount < conf.Retries; tryCount++)
        {
            deserializer.Reset();
            try
            {
                if (conf.Send(sbuf))
                {
                    var timeout = DateTime.Now.AddMilliseconds(conf.Timeout + extTimeout);
                    while (timeout > DateTime.Now)
                    {
                        Thread.Sleep(20);
                        if (conf.TryGetAnswerWithFixedTimeout(out var r, 100))
                        {
                            foreach (var b in r)
                            {
                                var answer = deserializer.Deserialize(b);
                                if (answer == null)
                                    continue;
                                if (!cmd.IsMyAnswer(answer))
                                {
                                    deserializer.Reset();
                                    continue;
                                }
                                if (!answer.IsPasswordAccepted)
                                    return false;
                                if (CheckIsSuccess == null)
                                    CheckIsSuccess = p => p.Data != null && p.Data.Arg2 != FalseValue;
                                Answer = answer;
                                return CheckIsSuccess(answer);
                            }
                        }
                    }
                }
                else continue;
            }
            catch (TimeoutException)
            {
                continue;
            }
        }
        return false;
    }

    public static bool Read(ISendAndGetAnswerConfig conf, int addr, int len, [NotNullWhen(true)] out byte[]? Data)
    {
        Data = null;
        var data = new byte[len];
        var cmd = new CommandPack((int)CommandCode.ReadAny, 0, 0);
        for (int offset = 0, psize; offset < len; offset += psize)
        {
            psize = Math.Min(Pack.MaxDataLength, len - offset);
            cmd.Arg1 = addr + offset;
            cmd.Arg2 = psize;
            if (!Exec(conf, out var ack, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2 && r.Data?.Arg3?.Length == cmd.Arg2))
                return false;
            if (ack.Data?.Arg3 == null)
                return false;
            ack.Data.Arg3.CopyTo(data.AsSpan(offset));
        }
        Data = data;
        return Data != null;
    }

    public static bool Write(ISendAndGetAnswerConfig conf, int addr, ReadOnlySpan<byte> data)
    {
        if (addr % FlashPageSize != 0)
            throw new ArgumentException("Address must be page-aligned.", nameof(addr));
        var cmd = new CommandPack((int)CommandCode.WriteAny, 0, 0);
        var buf = new byte[Pack.MaxDataLength];
        for (int offset = 0, psize; offset < data.Length; offset += psize)
        {
            psize = Math.Min(Pack.MaxDataLength, data.Length - offset);
            cmd.Arg1 = addr + offset;
            cmd.Arg2 = psize;
            if (psize == Pack.MaxDataLength)
            {
                data.Slice(offset, psize).CopyTo(buf);
                cmd.Arg3 = buf;
            }
            else cmd.Arg3 = data.Slice(offset, psize).ToArray();
            if (!Exec(conf, out _, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2))
                return false;
        }
        return true;
    }

    static readonly byte[] ErasePlaceholdData = new byte[] { 0xFF };
    public static bool Erase(ISendAndGetAnswerConfig conf, int addr, int len)
    {
        if (addr % FlashPageSize != 0)
            throw new ArgumentException("Address must be page-aligned.", nameof(addr));
        if (len % FlashPageSize != 0)
            throw new ArgumentException("Length must be an integer multiple of page size.", nameof(len));
        var pgcount = len / FlashPageSize;
        var cmd = new CommandPack((int)CommandCode.WriteAny, 0, ErasePlaceholdData.Length, ErasePlaceholdData);
        for (var i = 0; i < pgcount; i++)
        {
            cmd.Arg1 = addr + i * FlashPageSize;
            if (!Exec(conf, out _, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2))
                return false;
        }
        return true;
    }

    public static bool Reboot(ISendAndGetAnswerConfig conf)
    => Exec(conf, out _, new((int)CommandCode.Reboot, 0, 0), r => r.Data?.Arg2 == 1);

    public static bool GetVersion(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out VersionInfo? Version)
    {
        Version = null;
        if (!Read(conf, VersionInfoAddress, VersionInfo.SizeConst, out var data))
            return false;
        Version = VersionInfo.Deserialize(data);
        if (Version == null)
            return false;
        var ver = Version.Value;
        return ver.Identifier == ProgramInfo.Identifier
                && ver.VersionID == ProgramInfo.VersionID;
    }

    public static bool GetBrightFixTable(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Dictionary<int, int>? Table)
    {
        Table = null;
        if (!Read(conf, BrightFixTableAddress, BrightFixTable.TableSize, out var data))
            return false;
        if (data.All(x => x == 0xFF))
        {
            Table = new();
            return true;
        }
        Table = BrightFixTable.GetTable(data);
        return Table != null;
    }

    public static bool SetBrightFixTable(ISendAndGetAnswerConfig conf, IReadOnlyDictionary<int, int> table)
    {
        return BrightFixTable.GenTableData(table, out var tabd)
            && Write(conf, BrightFixTableAddress, tabd);
    }

    public static bool ResetBrightFixTable(ISendAndGetAnswerConfig conf)
    {
        return Erase(conf, BrightFixTableAddress, BrightFixTable.TableSize.SizeAlignTo(FlashPageSize));
    }

    public static bool GetConfigs(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out string? Configs)
    {
        Configs = null;
        var data = new byte[ConfigSize];
        var len = 0;
        for (int offset = 0, psize; offset < ConfigSize; offset += psize)
        {
            psize = Math.Min(ConfigSize - offset, MaxDataLength);
            if (!Read(conf, ConfigAddress + offset, psize, out var buf))
                return false;

            var end = false;
            foreach (var b in buf)
            {
                if (b != '\0' && b != '\xFF')
                    data[len++] = b;
                else
                {
                    end = true;
                    break;
                }
            }
            if (end)
                break;
        }
        Configs = Config.GetConfig(data.AsSpan()[..len]);
        return true;
    }

    public static bool SetConfigs(ISendAndGetAnswerConfig conf, string configs)
    {
        return Write(conf, ConfigAddress, Config.GenConfigData(configs));
    }
}
