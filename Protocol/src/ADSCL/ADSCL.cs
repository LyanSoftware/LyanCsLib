using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common;
using System.Diagnostics.CodeAnalysis;

namespace Lytec.Protocol
{
    public static partial class ADSCL
    {
        public const Endian DefaultEndian = Endian.Little;
        public const CharSet DefaultCharSet = CharSet.Ansi;
        public static Encoding DefaultEncode { get; set; } = Encoding.GetEncoding(936);

        public const int FalseValue = -1;
        public const string NewLine = "\r\n";
        public const int NameSize = 16;
        public const int MaxDataLength = 1024;
        public const int GBufferSize = 2 * 1024 * 1024;
        public const string ConfigFileName = "CONFIG.LY";
        public const string OtherConfigFileName = "SYSTEM.LY";
        public const string PlaylistFileName = "PLAYLIST.LY";

        public const string SuPw = "LyTeClYtEc";

        public const int FlashWriteMaxFileSeconds = 50; // Flash写入最大文件所需时间
        public const int FlashReadMaxFileSeconds = 24; // Flash读取最大文件所需时间（暂停播放时为16秒）
        public const int FlashWriteBytesPerSecond = GBufferSize / FlashWriteMaxFileSeconds; // Flash写入速度
        public const int FlashReadBytesPerSecond = GBufferSize / FlashReadMaxFileSeconds; // Flash读取速度

        [Serializable]
        [Endian(DefaultEndian)]
        public readonly struct AllConfigs
        {
            public const int SizeConst = MacConfig.SizeConst + NetConfig.SizeConst + LEDConfig.SizeConst;
            static AllConfigs() => Debug.Assert(Marshal.SizeOf<AllConfigs>() == SizeConst);
            public MacConfig Mac { get; }
            public NetConfig Net { get; }
            public LEDConfig Led { get; }
        }

        public static byte[] InitFlashDataBlock(int size, byte fillByte = 0xFF) => GetFlashDataBlock(size, fillByte).ToArray();
        public static IEnumerable<byte> GetFlashDataBlock(int size, byte fillByte = 0xFF) => Enumerable.Repeat(fillByte, size);

        public static byte[] GetFixedLengthStringWithFlash(string str, int size, bool addEnd = true)
        {
            if (str == null)
                str = "";
            var r = str.SelectMany(c => DefaultEncode.GetBytes(new char[] { c }));
            if (addEnd && str.Length > 0)
                r = r.Take(size - 1).Append<byte>(0);
            else r = r.Take(size);
            r = r.Concat(GetFlashDataBlock(size)).Take(size);
            return r.ToArray();
        }

        public static string GetStringFromFixedLength(byte[] str)
        {
            if (str == null || str.Length == 0 || str[0] == '\0')
                return "";
            return DefaultEncode.GetString(str.TakeWhile(c => c != '\0').ToArray());
        }

        public static bool Exec(ISendAndGetAnswerConfig conf, [NotNullWhen(true)] out Pack? Answer, CommandPack command, string? password = null, Func<Pack, bool>? CheckIsSuccess = default, int extTimeout = 0)
        {
            var cmd = new Pack()
            {
                AddrCode = (byte)(conf.AddrCode ?? 0),
                Data = command.Clone(),
                Identifier = Pack.SendIdentifier.ToArray(),
            };
            if (!password.IsNullOrEmpty())
                cmd.Password = Pack.PasswordConverter.Convert(password);
            cmd.UpdatePackIndex();
            cmd.UpdateCheckSum();
            var sbuf = cmd.Serialize();
            var deserializer = Pack.CreateDeserializer();
            Answer = null;
            for (var tryCount = -1; tryCount < conf.Retries; tryCount++)
            {
                deserializer.Reset();
                byte[] recv;
                try
                {
                    if (conf.SendAndGetAnswer(sbuf, out var r, extTimeout))
                        recv = r;
                    else continue;
                }
                catch (TimeoutException)
                {
                    continue;
                }
                var answer = deserializer.Deserialize(recv);
                if (answer == null || !cmd.IsMyAnswer(answer))
                    continue;
                Answer = answer;
                if (CheckIsSuccess == null)
                    CheckIsSuccess = p => p.Data != null && p.Data.Arg2 != FalseValue;
                return Answer != null && CheckIsSuccess(Answer);
            }
            return false;
        }

        public static bool LoadFrom(ISendAndGetAnswerConfig config, int addr, int length, out byte[]? Data, Func<Pack, bool>? CheckIsValidData = null, string? password = null)
        {
            Data = null;
            if (!Exec(
                    config,
                    out var ans,
                    new CommandPack((int)CommandCode.LoadFrom, addr, length),
                    password,
                    r => r.Data != null && r.Data.Arg2 != FalseValue && (CheckIsValidData == null || CheckIsValidData(r))
                    ))
                return false;
            Data = ans.Data?.Arg3;
            return true;
        }

        public static bool LoadFrom(ISendAndGetAnswerConfig config, int addr, int length, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
        => LoadFrom(config, addr, length, out Data, r => r.Data != null && r.Data.Arg2 >= minDataLen && r.Data.Arg3?.Length >= minDataLen, password);

        public static bool GetSpStructInternal(ISendAndGetAnswerConfig config, SpStructIndex index, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
        => LoadFrom(config, (int)StructAddress.LoadFromSpStructs, (int)index, out Data, minDataLen, password);

        //public static bool GetVersionCode(ISendAndGetAnswerConfig config, out SCL.VersionCode Version, string password = null)
        //=> GetSpStruct(config, SCL.SpStructIndex.FullVersionCode, out Version, SCL.VersionCode.SizeConst, password);

        public static bool GetSpStruct<T>(ISendAndGetAnswerConfig config, SpStructIndex index, [NotNullWhen(true)] out T? data, int minDataLen, string? password = null)
        {
            var ret = GetSpStructInternal(config, index, out var bytes, minDataLen, password);
            data = ret ? bytes!.ToStruct<T>(0, DefaultEndian) : default;
            return ret;
        }

        public static bool GetAllConfigs(ISendAndGetAnswerConfig config, out AllConfigs Configs, string? password = null)
        => GetSpStruct(config, SpStructIndex.AllConfigs, out Configs, AllConfigs.SizeConst, password);

        public static bool GetMacConfig(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out MacConfig? Config, string? password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Mac : default;
            return ret;
        }
        
        public static bool GetNetConfig(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out NetConfig? Config, string? password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Net : default;
            return ret;
        }
        
        public static bool GetLedConfig(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out LEDConfig? Config, string? password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Led : default;
            return ret;
        }

        public static bool SendData(ISendAndGetAnswerConfig config, int addr, IEnumerable<byte> data, string? password = null, int extTimeout = 0)
        {
            while (data.Any())
            {
                var buf = data.Take(MaxDataLength).ToArray();
                data = data.Skip(MaxDataLength);
                if (!Exec(config, out _, new CommandPack((int)CommandCode.SendData, addr, buf.Length, buf), password, r => r.Data != null && r.Data.Arg2 == buf.Length, extTimeout))
                    return false;
            }
            return true;
        }

        public static bool SaveTo(ISendAndGetAnswerConfig config, int addr, int length, string? password = null, int extTimeout = 0)
        => Exec(config, out _, new CommandPack((int)CommandCode.SaveTo, addr, length), password, r => r.Data != null && r.Data.Arg2 == length, extTimeout);

        public static bool SetLEDConfig(ISendAndGetAnswerConfig config, LEDConfig conf, string? password = null)
        => SendData(config, 0, conf.ToBytes(), password)
            && SaveTo(config, (int)StructAddress.LEDConfig, LEDConfig.SizeConst, password);

        /// <summary>
        /// 格式化磁盘，仅支持内置存储（A盘）和RAM内存盘（C盘）
        /// </summary>
        /// <param name="config">通信配置</param>
        /// <param name="drv">目标磁盘</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool FormatDisk(ISendAndGetAnswerConfig config, DiskDriver drv, string? password = null)
        => Exec(config, out _, new CommandPack((int)CommandCode.FormatDisk, (int)drv, 0), password);

        /// <summary>
        /// 重新播放节目表
        /// </summary>
        /// <param name="config"></param>
        /// <param name="driver">节目表所在磁盘</param>
        /// <param name="index">节目表索引</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Replay(ISendAndGetAnswerConfig config, DiskDriver driver, int index, string? password = null)
        => Exec(config, out _, new CommandPack((int)CommandCode.Reset, 0, ((index & 0xff) << 24) | ((int)driver << 16)), password);

        /// <summary>
        /// 重启设备
        /// </summary>
        /// <param name="config"></param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Reboot(ISendAndGetAnswerConfig config, string? password = null)
        => Exec(config, out _, new CommandPack((int)CommandCode.Reset, 1, 0), password);

    }
}
