using System.Runtime.InteropServices;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lytec.Common.Data;
using Lytec.Common.Number;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        /// <summary>
        /// 串口配置
        /// </summary>
        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct UartConfig
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public enum StopBit : ushort
            {
                None = 0,
                Bits_1 = 1,
                Bits_2 = 2,
                Bits_1_Point_5 = 3,
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum CheckBit : ushort
            {
                None = 0,
                Even = 1,
                Odd = 2,
            }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum Protocols : ushort
            {
                Simple = 0,
                SCLStandard = 1,
                TS_AVS05 = 2,
                KYX_24G_S001 = 3,
                NMEA_0183 = 4,
                WuXiNewSkySensor = 5,
                LyFoglight = 6,
            }

            /// <summary>
            /// 波特率
            /// </summary>
            public uint Baudrate { get; set; }
            /// <summary>
            /// 数据位
            /// </summary>
            public ushort DataBits { get; set; }
            /// <summary>
            /// 停止位
            /// </summary>
            public StopBit StopBits { get; set; }
            public CheckBit Check { get; set; }
            /// <summary>
            /// 使用的协议
            /// </summary>
            public Protocols Protocol { get; set; }
            /// <summary>
            /// 双数据校验
            /// </summary>
            public WORDBool DoubleDataCheck { get; set; }
            private readonly ushort unused;
        }

    }
}
