using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using Lytec.Protocol.Foglight;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

partial class SCL
{
    /// <summary>
    /// 雾灯设备类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FoglightType : byte
    {
        [Description("禁用")]
        Invalid = 0,
        /// <summary>
        /// 深圳海一帆
        /// </summary>
        [Description("v1")]
        HaiYiFan = 1,
        /// <summary>
        /// 深圳海一帆（旧版）
        /// </summary>
        [Description("v2")]
        HaiYiFan_Old = 2,
        /// <summary>
        /// 深圳励研
        /// </summary>
        [Description("Ly")]
        Lyan = 3,
    }

    /// <summary>
    /// 雾灯设备串口
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FoglightUart : ushort
    {
        [Description("禁用")]
        Invalid = 0,
        COM1 = 1,
        COM2 = 2
    }

    /// <summary>
    /// 雾灯模式
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FoglightMode : ushort
    {
        [Description("禁用")]
        Invalid = 0,
        [Description("根据能见度自动切换")]
        Auto = 1,
        [Description("防止追尾警示（低能见度）模式")]
        Low = 2,
        [Description("行车主动诱导（中能见度）模式")]
        Middle = 3,
        [Description("道路轮廓强化（高能见度）模式")]
        High = 4,
        [Description("超高能见度模式")]
        VeryHigh = 5,
        [Description("关闭")]
        Close = VeryHigh
    }

    /// <summary>
    /// 雾灯参数
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(Constants.DefaultEndian)]
    public class FoglightArgs : IPackage, ICloneable
    {
        public const int SizeConst = 64;
        public const int MaxWLSendCount = 15;
        public const int MinWLSendCount = 1;
        public const int MaxWLChannelCount = 16;
        public const int MinWLChannelCount = 8;
        public const int MaxWLTxPower = 22;
        public const int MinWLTxPower = -9;

        static FoglightArgs() => Debug.Assert(Marshal.SizeOf<FoglightArgs>() == SizeConst);

        public const int MinSize = sizeof(FoglightMode)
            + sizeof(FoglightUart)
            + sizeof(FoglightType)
            + sizeof(VisMonitorType)
            + sizeof(ushort) * 5; // 占空比、频率、低/中/高能见度阈值

        /// <summary> 模式 </summary>
        public FoglightMode Mode { get; set; } = FoglightMode.Close;
        /// <summary> 设备串口 </summary>
        public FoglightUart Uart { get; set; } = FoglightUart.Invalid;
        /// <summary> 设备类型 </summary>
        public FoglightType Type { get; set; } = FoglightType.Invalid;
        /// <summary> 能见度监测仪类型 </summary>
        public VisMonitorType MonitorType { get; set; } = VisMonitorType.Invalid;
        /// <summary> 占空比 </summary>
        public ushort Duty { get; set; } = 1;
        /// <summary> 频率 </summary>
        public ushort Frequency { get; set; } = 2;
        /// <summary> 低能见度阈值（米） </summary>
        public ushort LowVis { get; set; } = 0;
        /// <summary> 中能见度阈值（米） </summary>
        public ushort MiddleVis { get; set; } = 0;
        /// <summary> 高能见度阈值（米） </summary>
        public ushort HighVis { get; set; } = 0;

        /// <summary> 黄灯亮度, 海一帆雾灯不可用 </summary>
        public ushort YBright { get; set; } = 15;
        /// <summary> 红灯亮度, 海一帆雾灯不可用 </summary>
        public ushort RBright { get; set; } = 9;
        /// <summary> 红灯亮起时间, 海一帆雾灯不可用 </summary>
        public ushort RedLightUpDuration { get; set; } = 5000;

        public Mode LyFoglightMode { get; set; } = Foglight.Mode.Normal;
        /// <summary> 开关配置, 海一帆雾灯不可用 </summary>
        public OptionFlags OptionFlags { get; set; } = OptionFlags.None;
        /// <summary> 无线通讯连发间隔(ms), 海一帆雾灯不可用 </summary>
        public ushort WLSendInterval { get; set; } = 150;
        /// <summary> 无线通讯连发次数(1~15), 海一帆雾灯不可用 </summary>
        public int WLSendCount
        {
            get => _WLSendCount;
            set => _WLSendCount = (ushort)Math.Max(Math.Min(value, MaxWLSendCount), MinWLSendCount);
        }
        private ushort _WLSendCount = 3;
        /// <summary> 无线通讯超时时间(ms), 海一帆雾灯不可用 </summary>
        public ushort WLTimeout { get; set; } = 360;
        /// <summary> 无线通讯最大雾灯模块节点数, 海一帆雾灯不可用 </summary>
        public ushort MaxNodeCount { get; set; } = 10;
        /// <summary> 跨站连接最大跨越节点数量上限 </summary>
        public ushort MaxSkipNode { get; set; } = 4;
        /// <summary> 无线通讯前导填充字节数, 海一帆雾灯不可用 </summary>
        public ushort LeaderPadding { get; set; } = 0;
        /// <summary> 无线通讯定期刷新链路间隔时间(分钟), 海一帆雾灯不可用 </summary>
        public ushort SeekCallInterval { get; set; } = 60;
        /// <summary> GPS休眠时间(分钟), 海一帆雾灯不可用 </summary>
        public ushort GPSSleepTime { get; set; } = 60;
        /// <summary> 自动亮度时的同步亮度间隔（分钟） </summary>
        public ushort AutoBrightSyncInterval { get; set; } = 15;
        /// <summary> 自动亮度时的亮度上限（百分比） </summary>
        public byte AutoBrightMaxPercent { get; set; } = 100;
        /// <summary> 自动亮度时的亮度下限（百分比） </summary>
        public byte AutoBrightMinPercent { get; set; } = 0;
        /// <summary> 无线最大信道数（8～16） </summary>
        public int WLMaxChannel
        {
            get => _WLMaxChannel;
            set => _WLMaxChannel = (byte)Math.Max(Math.Min(value, MaxWLChannelCount), MinWLChannelCount);
        }
        private byte _WLMaxChannel = 16;
        /// <summary> 无线信道（0～最大信道数-1） </summary>
        public byte WLChannel { get; set; } = 0;
        /// <summary> 无线发射功率（-9～22dbm） </summary>
        public int WLTxPower
        {
            get => _WLTxPower - 9;
            set => _WLTxPower = (byte)(Math.Max(Math.Min(value, MaxWLTxPower), MinWLTxPower) + 9);
        }
        private byte _WLTxPower = 14;

        /// <summary>
        /// 普通指令（非维护网络指令）通讯超时重发次数
        /// </summary>
        public byte DataTransferRetries { get; set; } = 3;

        public byte TimerOnHour { get; set; } = 0;
        public byte TimerOnMinute { get; set; } = 0;
        public byte TimerOffHour { get; set; } = 23;
        public byte TimerOffMinute { get; set; } = 59;

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct LowPowerAction
        {
            /// <summary> 电量阈值百分比 </summary>
            public byte Threshold { get; set; }
            /// <summary> 低于阈值时的亮度百分比 </summary>
            public byte Bright { get; set; }
        }
        /// <summary>
        /// 低电量操作
        /// </summary>
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
        public LowPowerAction[] LowPowerActions { get; set; } = new LowPowerAction[5];

        public byte[] Serialize() => this.ToBytes();

        public static FoglightArgs? Deserialize(byte[] bytes, int offset = 0)
        {
            var len = bytes.Length - offset;
            if (len < MinSize)
                return null;
            var buf = bytes;
            if (len < SizeConst)
            {
                buf = new byte[SizeConst];
                Array.Copy(bytes, offset, buf, 0, len);
                offset = 0;
            }
            return buf.ToStruct<FoglightArgs>(offset);
        }

        object ICloneable.Clone() => Clone();
        public FoglightArgs Clone() => Deserialize(Serialize())!;
    }
}
