using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lytec.Common.Data;

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

            GetRuntimeInfo = ReadAny,
            LoadFrom = ReadAny
            #endregion

        }

    }
}
