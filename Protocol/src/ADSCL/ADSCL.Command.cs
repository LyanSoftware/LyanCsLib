using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using System.Diagnostics.CodeAnalysis;
using Lytec.Common;
using Lytec.Common.Serialization;
using System.Buffers;

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

        static DeviceContext GetSharedContext(ISendAndGetAnswerConfig conf, string? password = null)
        {
            var context = Pack.MakeContext(conf);
            if (password != null)
                context.SetPassword(password);
            else context.UnsetPassword();
            return context;
        }

        public static bool Exec(
            ISendAndGetAnswerConfig conf,
            [NotNullWhen(true)] out Pack? Answer,
            CommandPack command,
            Func<Pack, bool>? CheckIsSuccess = default,
            int extTimeout = 0,
            string? password = null
            )
        => Exec(GetSharedContext(conf, password), out Answer, command, CheckIsSuccess, extTimeout);
        public static bool Exec(
            DeviceContext context,
            [NotNullWhen(true)] out Pack? Answer,
            CommandPack command,
            Func<Pack, bool>? CheckIsSuccess = default,
            int extTimeout = 0
            )
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
                        var rcvTime = DateTime.Now;
                        while (timeout > DateTime.Now)
                        {
                            Thread.Sleep(20);
                            if (conf.TryGetAnswer(out var r, extTimeout))
                            {
                                rcvTime = DateTime.Now;
                                Pack? answer = null;
                                if (conf.IsStream)
                                {
                                    foreach (var b in r)
                                    {
                                        if (deserializer.Append(b, out answer).Status != OperationStatus.Done
                                            || answer == null)
                                            continue;
                                        if (!cmd.IsMyAnswer(answer))
                                        {
                                            deserializer.Reset();
                                            continue;
                                        }
                                    }
                                }
                                else context.Codec.ParseDatagram(r, out answer);
                                if (answer == null || !context.IsPasswordAccepted(answer))
                                    return false;
                                if (CheckIsSuccess == null)
                                    CheckIsSuccess = p => p.Data != null && p.Data.Arg2 != FalseValue;
                                Answer = answer;
                                return CheckIsSuccess(answer);
                            }
                            else if ((DateTime.Now - rcvTime).TotalMilliseconds > 500)
                                deserializer.Reset();
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

        public static bool LoadFrom(ISendAndGetAnswerConfig config, int addr, int length, out byte[]? Data, Func<Pack, bool>? CheckIsValidData = null, string? password = null)
        => LoadFrom(GetSharedContext(config, password), addr, length, out Data, CheckIsValidData);
        public static bool LoadFrom(DeviceContext context, int addr, int length, out byte[]? Data, Func<Pack, bool>? CheckIsValidData = null)
        {
            Data = null;
            if (!Exec(
                    context,
                    out var ans,
                    new CommandPack((int)CommandCode.LoadFrom, addr, length),
                    r => r.Data != null && r.Data.Arg2 != FalseValue && (CheckIsValidData == null || CheckIsValidData(r))
                    ))
                return false;
            Data = ans.Data?.Arg3;
            return true;
        }

        public static bool LoadFrom(ISendAndGetAnswerConfig config, int addr, int length, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
        => LoadFrom(GetSharedContext(config, password), addr, length, out Data, minDataLen);
        public static bool LoadFrom(DeviceContext context, int addr, int length, [NotNullWhen(true)] out byte[]? Data, int minDataLen)
        => LoadFrom(context, addr, length, out Data, r => r.Data != null && r.Data.Arg2 >= minDataLen && r.Data.Arg3?.Length >= minDataLen);

        public static bool GetSpStructInternal(ISendAndGetAnswerConfig config, SpStructIndex index, [NotNullWhen(true)] out byte[]? Data, int minDataLen, string? password = null)
        => GetSpStructInternal(GetSharedContext(config, password), index, out Data, minDataLen);
        public static bool GetSpStructInternal(DeviceContext context, SpStructIndex index, [NotNullWhen(true)] out byte[]? Data, int minDataLen)
        => LoadFrom(context, (int)StructAddress.LoadFromSpStructs, (int)index, out Data, minDataLen);

        public static bool GetSpStruct<T>(ISendAndGetAnswerConfig config, SpStructIndex index, [NotNullWhen(true)] out T? data, int minDataLen, string? password = null)
        => GetSpStruct<T>(GetSharedContext(config, password), index, out data, minDataLen);
        public static bool GetSpStruct<T>(DeviceContext context, SpStructIndex index, [NotNullWhen(true)] out T? data, int minDataLen)
        {
            var ret = GetSpStructInternal(context, index, out var bytes, minDataLen);
            data = ret ? bytes!.ToStruct<T>(0, DefaultEndian) : default;
            return ret;
        }

        public static bool GetAllConfigs(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out AllConfigs? Configs, string? password = null)
        => GetAllConfigs(GetSharedContext(config, password), out Configs);
        public static bool GetAllConfigs(DeviceContext context, [NotNullWhen(true)] out AllConfigs? Configs)
        {
            var ret = GetSpStructInternal(context, SpStructIndex.AllConfigs, out var bytes, AllConfigs.SizeConst);
            Configs = null;
            if (!ret)
                return false;
            Configs = new AllConfigs(
                bytes.AsSpan()[..LEDConfig.SizeConst].ToStruct<LEDConfig>(),
                bytes.AsSpan()[LEDConfig.SizeConst..].ToStruct<NetConfig>()
                );
            return true;
        }

        public static bool GetNetConfig(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out NetConfig? Config, string? password = null)
        => GetNetConfig(GetSharedContext(config, password), out Config);
        public static bool GetNetConfig(DeviceContext context, [NotNullWhen(true)] out NetConfig? Config)
        {
            if (GetAllConfigs(context, out var cfgs))
            {
                Config = cfgs.Net;
                return true;
            }
            Config = default;
            return false;
        }

        public static bool GetLedConfig(ISendAndGetAnswerConfig config, [NotNullWhen(true)] out LEDConfig? Config, string? password = null)
        => GetLedConfig(GetSharedContext(config, password), out Config);
        public static bool GetLedConfig(DeviceContext context, [NotNullWhen(true)] out LEDConfig? Config)
        {
            if (GetAllConfigs(context, out var cfgs))
            {
                Config = cfgs.Led;
                return true;
            }
            Config = default;
            return false;
        }

        public static bool SendData(ISendAndGetAnswerConfig config, int addr, IEnumerable<byte> data, string? password = null, int extTimeout = 0)
        => SendData(GetSharedContext(config, password), addr, data, extTimeout);
        public static bool SendData(DeviceContext context, int addr, IEnumerable<byte> data, int extTimeout = 0)
        {
            while (data.Any())
            {
                var buf = data.Take(MaxDataLength).ToArray();
                data = data.Skip(MaxDataLength);
                if (!Exec(context, out _, new CommandPack((int)CommandCode.SendData, addr, buf.Length, buf), r => r.Data != null && r.Data.Arg2 == buf.Length, extTimeout))
                    return false;
            }
            return true;
        }

        public static bool SaveTo(ISendAndGetAnswerConfig config, int addr, int length, string? password = null, int extTimeout = 0)
        => SaveTo(GetSharedContext(config, password), addr, length, extTimeout);
        public static bool SaveTo(DeviceContext context, int addr, int length, int extTimeout = 0)
        => Exec(context, out _, new CommandPack((int)CommandCode.SaveTo, addr, length), r => r.Data != null && r.Data.Arg2 == length, extTimeout);

        public static bool GetFileSize(ISendAndGetAnswerConfig config, DiskDriver disk, string filepath, out int FileSize, string? password = null, int extTimeout = 0)
        => GetFileSize(GetSharedContext(config, password), disk, filepath, out FileSize, extTimeout);
        public static bool GetFileSize(DeviceContext context, DiskDriver disk, string filepath, out int FileSize, int extTimeout = 0)
        {
            FileSize = -1;
            if (Exec(
                context,
                out var p,
                new CommandPack((int)CommandCode.LoadFileToBuff, (int)disk | (3 << 2), 0, new byte[4].Concat(ToFixedLengthString(filepath, 32)).ToArray()),
                r => r.Data != null && r.Data.Arg2 != 0 && r.Data.Arg2 != FalseValue,
                extTimeout))
            {
                FileSize = p.Data!.Arg2;
                return true;
            }
            return false;
        }

        public static bool GetFileMD5(ISendAndGetAnswerConfig config, DiskDriver disk, string filepath, [NotNullWhen(true)] out string? md5, int fileSize = -1, string? password = null, int extTimeout = 0)
        => GetFileMD5(GetSharedContext(config, password), disk, filepath, out md5, fileSize, extTimeout);
        public static bool GetFileMD5(DeviceContext context, DiskDriver disk, string filepath, [NotNullWhen(true)] out string? md5, int fileSize = -1, int extTimeout = 0)
        {
            md5 = null;
            if (fileSize <= 0)
            {
                if (!GetFileSize(context, disk, filepath, out fileSize, extTimeout))
                    return false;
            }
            if (Exec(
                context,
                out var p,
                new CommandPack((int)CommandCode.LoadFileToBuff, (int)disk | (2 << 2), 0, new byte[4].Concat(ToFixedLengthString(filepath, 32)).ToArray()),
                r => r.Data?.Arg2 == 16 && r.Data.Arg3.Length == 16,
                extTimeout + (fileSize / CalcFileMd5SpeedPerSecond * 1000) + 1000))
            {
                md5 = p.Data!.Arg3.ToArray().ToHex("");
                return true;
            }
            return false;
        }

        public static bool SetLEDConfig(ISendAndGetAnswerConfig config, LEDConfig conf, string? password = null)
        => SetLEDConfig(GetSharedContext(config, password), conf);
        public static bool SetLEDConfig(DeviceContext context, LEDConfig conf)
        => SendData(context, 0, conf.ToBytes())
            && SaveTo(context, (int)StructAddress.LEDConfig, LEDConfig.SizeConst);

        /// <summary>
        /// 格式化磁盘，仅支持内置存储（A盘）和RAM内存盘（C盘）
        /// </summary>
        /// <param name="config">通信配置</param>
        /// <param name="disk">目标磁盘</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool FormatDisk(ISendAndGetAnswerConfig config, DiskDriver disk, string? password = null)
        => FormatDisk(GetSharedContext(config, password), disk);
        public static bool FormatDisk(DeviceContext context, DiskDriver disk)
        => Exec(context, out _, new CommandPack((int)CommandCode.FormatDisk, (int)disk, 0));

        /// <summary>
        /// 重新播放节目表
        /// </summary>
        /// <param name="config"></param>
        /// <param name="disk">节目表所在磁盘</param>
        /// <param name="index">节目表索引</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Replay(ISendAndGetAnswerConfig config, DiskDriver disk, int index, string? password = null)
        => Replay(GetSharedContext(config, password), disk, index);
        public static bool Replay(DeviceContext context, DiskDriver disk, int index)
        => Exec(context, out _, new CommandPack((int)CommandCode.Reset, 0, ((index & 0xff) << 24) | ((int)disk << 16)));

        /// <summary>
        /// 重启设备
        /// </summary>
        /// <param name="config"></param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Reboot(ISendAndGetAnswerConfig config, string? password = null)
        => Reboot(GetSharedContext(config, password));
        public static bool Reboot(DeviceContext context)
        => Exec(context, out _, new CommandPack((int)CommandCode.Reset, 1, 0));

    }
}
