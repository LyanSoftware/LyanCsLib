using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using System.Diagnostics.CodeAnalysis;
using Lytec.Common;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Endian(DefaultEndian)]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CommandCode : uint
        {
            SendData = 0x00000000,
            GetData = 0x00000001,
            SaveToFile = 0x00000002,
            LoadFileToBuff = 0x00000003,
            DeleteFile = 0x00000004,
            GetDiskFreeSpace = 0x00000005,
            ReadDir = 0x00000006,
            FormatDisk = 0x00000007,
            _Reserved0 = 0x00000008,
            SetClock = 0x00000009,
            TurnVGA = 0x0000000a,
            SetPlayStatus = TurnVGA,
            SetBright = 0x0000000b,
            SetSwitch = 0x0000000c,
            SetLEDPower = SetSwitch,
            MakeDir = 0x0000000d,
            DeleteDir = 0x0000000e,
            ShowString = 0x0000000f,
            GetLastResult = 0x00000010,
            _Reserved1 = 0x00000011,
            GetPlayInfo = 0x00000012,
            DirectDraw = 0x00000013,
            PowerDotCheck = 0x00000014,
            SendSmallFile = 0x00000015,
            SendToUart = 0x00000016,
            RemoteControl = 0x00000017,
            DHCPConfig = 0x00000018,
            Rename = 0x00000019,
            Reset = 0x000055aa,

            #region 附加协议指令

            FoglightConfig = 0x40, // 读写雾灯配置

            #endregion

            #region 不开放给用户的部分

            Seek = 0x00000080,
            SetFPGAParam = 0x00000081,
            SetHardwave = 0x00000082,
            SaveTo = 0x00000083,
            ReadAny = 0x00000084,

            LoadFrom = ReadAny,
            #endregion

            GetRuntimeInfo = ReadAny,
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

        public static bool GetAllConfigs(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out AllConfigs? Configs, string? password = null)
        {
            var ret = GetSpStructInternal(config, SpStructIndex.AllConfigs, out var bytes, AllConfigs.SizeConst, password);
            Configs = null;
            if (!ret)
                return false;
            Configs = new AllConfigs(
                bytes.Take(LEDConfig.SizeConst).ToStruct<LEDConfig>(),
                bytes.Skip(LEDConfig.SizeConst).ToStruct<NetConfig>()
                );
            return true;
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
