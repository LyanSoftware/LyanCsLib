using System;
using Lytec.Common;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.Diagnostics.CodeAnalysis;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.ADSCL;
using System.Text;
using static Lytec.Protocol.LyLCDTvBrt.ProgramInfo;
using Lytec.Protocol.Ly;
using Lytec.Common.Serialization;
using AdsclPack = Lytec.Protocol.ADSCL.Pack;
using System.Buffers;

namespace Lytec.Protocol.LyLCDTvBrt;

public class Command
{

    public class DeviceContext : DeviceContext<Pack, CommandPack>
    {
        public DeviceContext(ISendAndGetAnswerConfig commCfg, IPackCodec<Pack, CommandPack> codec, int password)
            : base(commCfg, codec, password)
        {
        }
    }

    public class Pack : Pack<Pack, CommandPack>
    {
        public Pack(PackDirection direction, byte addrCode, ushort packIndex, int password, CommandPack data)
            : base(direction, addrCode, packIndex, password, data)
        {
        }

        public const int MaxDataLength = 0x400;

        public static PackConfig Config { get; } = new(
            [(byte)'\x1b', (byte)'$', (byte)'T', (byte)'v', (byte)'B', (byte)'r', (byte)'t'],
            [(byte)'\x1b', (byte)'&', (byte)'t', (byte)'V', (byte)'b', (byte)'R', (byte)'T'],
            CommandPack.MinDataLength,
            CommandPack.MinDataLength + MaxDataLength
        );

        public static PackCodec<Pack, CommandPack> Codec { get; } = new(
            Config,
            BinaryResynchronizationMode.Automatic,
            new AdsclPack.CommandPackSerializer(),
            new AdsclPack.CommandPackDeserializer(),
            (dir, addr, pid, pw, data) => new Pack(dir, addr, pid, pw, data)
        );
        
        public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg)
        => new(cfg, Codec, Config.NoPassword);

        public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, int password)
        => new(cfg, Codec, password);

        public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, string password)
        => new(cfg, Codec, Config.PasswordConverter.Convert(password));

        public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, byte[] password)
        => new(cfg, Codec, Config.PasswordConverter.Convert(password));

    }

    [Endian(DefaultEndian)]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CommandCode : uint
    {
        ReadAny = 0,
        WriteAny = 1,
        Reboot = 0x10,
    }

    static DeviceContext GetSharedContext(ISendAndGetAnswerConfig conf) => Pack.MakeContext(conf);

    public static bool Exec(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Pack? Answer, CommandPack command, Func<Pack, bool>? CheckIsSuccess = default, int extTimeout = 0)
    => Exec(GetSharedContext(conf), out Answer, command, CheckIsSuccess, extTimeout);
    public static bool Exec(DeviceContext context, [NotNullWhen(true)] out Pack? Answer, CommandPack command, Func<Pack, bool>? CheckIsSuccess = default, int extTimeout = 0)
    {
        var conf = context.CommConfig;
        var cmd = context.CreateRequest(command);
        var sbuf = context.Codec.Serialize(cmd);
        var deserializer = context.Codec.CreateStreamDecoder();
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
                                if (deserializer.Append(b, out var answer).Status != OperationStatus.Done
                                    || answer == null)
                                    continue;
                                if (!cmd.IsMyAnswer(answer))
                                {
                                    deserializer.Reset();
                                    continue;
                                }
                                if (!context.IsPasswordAccepted(answer))
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
    => Read(GetSharedContext(conf), addr, len, out Data);
    public static bool Read(DeviceContext context, int addr, int len, [NotNullWhen(true)] out byte[]? Data)
    {
        Data = null;
        var data = new byte[len];
        var cmd = new CommandPack((int)CommandCode.ReadAny, 0, 0);
        for (int offset = 0, psize; offset < len; offset += psize)
        {
            psize = Math.Min(Pack.MaxDataLength, len - offset);
            cmd.Arg1 = addr + offset;
            cmd.Arg2 = psize;
            if (!Exec(context, out var ack, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2 && r.Data?.Arg3?.Length == cmd.Arg2))
                return false;
            if (ack.Data?.Arg3 == null)
                return false;
            ack.Data.Arg3.CopyTo(data.AsSpan(offset));
        }
        Data = data;
        return Data != null;
    }

    public static bool Write(ISendAndGetAnswerConfig conf, int addr, ReadOnlySpan<byte> data)
    => Write(GetSharedContext(conf), addr, data);
    public static bool Write(DeviceContext context, int addr, ReadOnlySpan<byte> data)
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
            if (!Exec(context, out _, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2))
                return false;
        }
        return true;
    }

    static readonly byte[] ErasePlaceholdData = [0xFF];
    public static bool Erase(ISendAndGetAnswerConfig conf, int addr, int len)
    => Erase(GetSharedContext(conf), addr, len);
    public static bool Erase(DeviceContext context, int addr, int len)
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
            if (!Exec(context, out _, cmd, r => r.Data?.Arg1 == cmd.Arg1 && r.Data?.Arg2 == cmd.Arg2))
                return false;
        }
        return true;
    }

    public static bool Reboot(ISendAndGetAnswerConfig conf)
    => Reboot(GetSharedContext(conf));
    public static bool Reboot(DeviceContext context)
    => Exec(context, out _, new((int)CommandCode.Reboot, 0, 0), r => r.Data?.Arg2 == 1);

    public static bool GetVersion(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out VersionInfo? Version)
    => GetVersion(GetSharedContext(conf), out Version);
    public static bool GetVersion(DeviceContext context, [NotNullWhen(true)] out VersionInfo? Version)
    {
        Version = null;
        if (!Read(context, VersionInfoAddress, VersionInfo.SizeConst, out var data))
            return false;
        Version = VersionInfo.Deserialize(data);
        if (Version == null)
            return false;
        var ver = Version.Value;
        return ver.Identifier == ProgramInfo.Identifier
                && ver.VersionID == ProgramInfo.VersionID;
    }

    public static bool GetBrightFixTable(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Dictionary<int, int>? Table)
    => GetBrightFixTable(GetSharedContext(conf), out Table);
    public static bool GetBrightFixTable(DeviceContext context, [NotNullWhen(true)] out Dictionary<int, int>? Table)
    {
        Table = null;
        if (!Read(context, BrightFixTableAddress, BrightFixTable.TableSize, out var data))
            return false;
        if (data.All(x => x == 0xFF))
        {
            Table = [];
            return true;
        }
        Table = BrightFixTable.GetTable(data);
        return Table != null;
    }

    public static bool SetBrightFixTable(ISendAndGetAnswerConfig conf, IReadOnlyDictionary<int, int> table)
    => SetBrightFixTable(GetSharedContext(conf), table);
    public static bool SetBrightFixTable(DeviceContext context, IReadOnlyDictionary<int, int> table)
    {
        return BrightFixTable.GenTableData(table, out var tabd)
            && Write(context, BrightFixTableAddress, tabd);
    }

    public static bool ResetBrightFixTable(ISendAndGetAnswerConfig conf)
    => ResetBrightFixTable(GetSharedContext(conf));
    public static bool ResetBrightFixTable(DeviceContext context)
    {
        return Erase(context, BrightFixTableAddress, BrightFixTable.TableSize.SizeAlignTo(FlashPageSize));
    }

    public static bool GetConfigs(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out string? Configs)
    => GetConfigs(GetSharedContext(conf), out Configs);
    public static bool GetConfigs(DeviceContext context, [NotNullWhen(true)] out string? Configs)
    {
        Configs = null;
        var data = new byte[ConfigSize];
        var len = 0;
        for (int offset = 0, psize; offset < ConfigSize; offset += psize)
        {
            psize = Math.Min(ConfigSize - offset, MaxDataLength);
            if (!Read(context, ConfigAddress + offset, psize, out var buf))
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
    => SetConfigs(GetSharedContext(conf), configs);
    public static bool SetConfigs(DeviceContext context, string configs)
    {
        return Write(context, ConfigAddress, Config.GenConfigData(configs));
    }
}
