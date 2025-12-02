using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.ComponentModel;
using System.Text.RegularExpressions;
using Lytec.Common.Number;
using System.Text;
using System.Security.Cryptography;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TestPlayType : ushort
    {
        Default = 0,
        None = 0x55,
        FourHighlightDots = 0xAA,
        DotByDot = 0x3C
    }

    /// <summary>
    /// 颜色顺序
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ColorOrder
    {
        RGB = 0,
        GRB = 1,
        RBG = 2,
        BRG = 3, // 数据中为GBR，实际效果为BRG，因此显示为BRG
        BGR = 4,
        GBR = 5  // 数据中为BRG，实际效果为GBR，因此显示为GBR
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum ScanType
    {
        Invalid = 0,
        [Description("Static")]
        Static = 1,
        [Description("1/2")]
        S02 = 2,
        [Description("1/4")]
        S04 = 4,
        [Description("1/8")]
        S08 = 8,
        [Description("1/16")]
        S16 = 16,
    }
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DataGroupHeight
    {
        Invalid = 0,
        P01 = 1,
        P02 = 2,
        P04 = 4,
        P08 = 8,
        P16 = 16,
    }

    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ControlRange : ushort
    {
        Range4032x128 = 0,
        Range2048x256 = 1,
        Range1024x512 = 2,
        Range2048x128 = 3,
        Range1024x256 = 4,
        Range512x512 = 5,
        Range1024x256FullColor = 6,
        Range1024x256FullColorCompact = 7
    }

    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum ClockDuty
    {
        Percent25 = 0,
        Percent50 = 1,
        Percent75 = 2
    }

    /// <summary>
    /// 亮度修正配置
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct BrightAmend
    {
        public const int DefaultMax = 150;
        public const int DefaultMin = 0;

        private ushort _Max;
        private ushort _Min;
        public int Max { get => _Max; set => _Max = (ushort)value; }
        public int Min { get => _Min; set => _Min = (ushort)value; }
        public override string ToString() => (Max == 0 || Max == ushort.MaxValue) ? $"Unconfigured(default: min={DefaultMin}), max={DefaultMax})" : $"(min={Min}, max={Max})";
    }

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum RouteOption : ushort
    {
        None = 0,
        All = ushort.MaxValue,
        Exchange1Column = 1 << 0,
        Exchange2Column = 1 << 1,
        Exchange4Column = 1 << 2,
        Exchange8Column = 1 << 3,
        ExchangeRedAndGreen = 1 << 4,
        ReducedClockFrequency = 1 << 5,
        DotCheckEnabled = ReducedClockFrequency,
        InverseEvenAndOddLineGroups = 1 << 6,
        InverseColorSignals = 1 << 7,
        Exchange1Row = 1 << 8,
        Exchange2Row = 1 << 9,
        Exchange4Row = 1 << 10,
        Exchange8Row = 1 << 11,
        DecreaseLineOrderBy1 = 1 << 12,
        ControllerAtLeftSide = 1 << 13,
        DecodeLineSignals = 1 << 14,
        IncreaseOutEnableSignal = 1 << 15,
    }

    [Serializable]
    [Endian(Constants.DefaultEndian)]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    public class RouteConfig : IPackage
    {
        public const int HeaderSizeConst = 64;
        public const int RouteDataLength = 1024;
        public const int SizeConst = HeaderSizeConst + RouteDataLength * sizeof(ushort);
        public const int IdentifierSize = 8;
        public const string Identifier = "SCLRoute";

        static RouteConfig() => Debug.Assert(Marshal.SizeOf<RouteConfig>() == SizeConst);

        public string Id
        {
            get => Encoding.ASCII.GetString(_Id);
            set => _Id = Encoding.ASCII.GetBytes(value);
        }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = IdentifierSize)]
        private byte[] _Id = Encoding.ASCII.GetBytes(Identifier);
        public ushort CheckSum { get; set; }
        public int Version { get; set; }
        public int MinCompatibleVersion { get; set; }
        public RouteOption RouteOptions { get; set; }
        public byte _ScanType;
        public ScanType ScanType { get => (ScanType)_ScanType; set => _ScanType = (byte)value; }
        public byte _DataGroups;
        public int DataGroups { get => _DataGroups; set => _DataGroups = (byte)value; }
        public ushort _ModuleWidth;
        public int ModuleWidth { get => _ModuleWidth; set => _ModuleWidth = (ushort)value; }
        public ushort _ModuleHeight { get; set; }
        public int ModuleHeight { get => _ModuleHeight; set => _ModuleHeight = (ushort)value; }
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 38)]
        private readonly byte[] _unused = new byte[38];
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = RouteDataLength)]
        public ushort[] RouteData { get; set; } = new ushort[RouteDataLength];

        public int RowBits => (int)Math.Log(ModuleHeight / DataGroups / (int)ScanType, 2);

        public int ColumnBits => RouteInfo.DataBits - RowBits;

        public int RowMask => (int)(BitHelper.MakeMask(RowBits) << ColumnBits);

        public int ColumnMask => (int)BitHelper.MakeMask(ColumnBits);

        public bool IsValid => Id == Identifier && CheckSum == GetCRC();

        public void RecalcCRC() => CheckSum = GetCRC();

        public ushort GetCRC() => CreateCRC16().Compute(Serialize().Skip(IdentifierSize + sizeof(ushort)));

        public byte[] Serialize() => this.ToBytes();

        public static IReadOnlyDictionary<long, long> AcceptFileSizes
        = new long[] { 2080, SizeConst }.ToDictionary(i => i);

        public static RouteConfig Deserialize(byte[] bytes, int offset = 0)
        {
            var dlen = bytes.Length - offset;
            switch (dlen)
            {
                case 2080:
                    var cfg = new RouteConfig()
                    {
                        Id = Identifier,
                        RouteOptions = bytes.ToStruct<RouteOption>(offset, DefaultEndian),
                        ScanType = (ScanType)bytes[offset + 2],
                        DataGroups = bytes[offset + 3],
                        ModuleWidth = bytes.ToStruct<ushort>(offset + 4, DefaultEndian),
                        ModuleHeight = bytes.ToStruct<ushort>(offset + 6, DefaultEndian),
                        RouteData = bytes.ToStruct<ushort[]>(offset + 32, DefaultEndian),
                    };
                    cfg.RecalcCRC();
                    return cfg;
                default:
                    if (dlen < SizeConst)
                        throw new IndexOutOfRangeException();
                    return bytes.ToStruct<RouteConfig>(offset);
            }
        }
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum RowDriveMode : byte
    {
        [Description("常规")]
        Normal = 0,
        [Description("行减一")]
        Dec1 = 1,
        [Description("行译码输出")]
        Decode = 2,
        [Description("时钟扫描")]
        ClockScan = 3,
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum AnalogInterfaceMode
    {
        Disabled = 0,
        VoltageDetect = 1,
        Invalid = 0xF,
    }
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

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum CheckColorMask : ushort
    {
        None = 0,
        Red = 1 << 0,
        Green = 1 << 1,
        Blue = 1 << 2,
        All = Red | Green | Blue,
    }

    public static readonly Regex ScanNameRegex = new Regex(@"^(?<Scan>\d{2})-P(?<Per>\d{2})(?:(?:-\[Manual\]-(?<ModuleWidth>\d{4})-(?<ModuleHeight>\d{4}))|(?:-.*?))?$", RegexOptions.Compiled);

    /// <summary>
    /// LED硬件配置
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    [Endian(DefaultEndian)]
    public struct LEDConfig : IPackage
    {
        public const int SizeConst = 256;

        static LEDConfig() => Debug.Assert(Marshal.SizeOf<LEDConfig>() == SizeConst);

        public const ushort FooterIdentifier = 0xAA55;
        public const byte MinBright = 0;
        public const byte MaxBright = 30;
        public const byte AutoBright = 31;

        public int RouteOptionValue { get; set; }
        public RouteOption RouteOption
        {
            get => (RouteOption)RouteOptionValue;
            set => RouteOptionValue = (RouteOptionValue & ~(int)RouteOption.All) | (int)value;
        }
        public bool Exchange1Column
        {
            get => BitHelper.GetFlag(RouteOptionValue, 0);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 0);
        }
        public bool Exchange2Column
        {
            get => BitHelper.GetFlag(RouteOptionValue, 1);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 1);
        }
        public bool Exchange4Column
        {
            get => BitHelper.GetFlag(RouteOptionValue, 2);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 2);
        }
        public bool Exchange8Column
        {
            get => BitHelper.GetFlag(RouteOptionValue, 3);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 3);
        }
        public int ColExchanges
        {
            get => BitHelper.GetValue(RouteOptionValue, 0, 4);
            set => BitHelper.SetValue(RouteOptionValue, value, 0, 4);
        }
        public bool ExchangeRedAndGreen
        {
            get => BitHelper.GetFlag(RouteOptionValue, 4);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 4);
        }
        public bool ReducedClockFrequency
        {
            get => BitHelper.GetFlag(RouteOptionValue, 5);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 5);
        }
        public bool DotCheckEnabled
        {
            get => ReducedClockFrequency;
            set => ReducedClockFrequency = value;
        }
        public bool InverseEvenAndOddLineGroups
        {
            get => BitHelper.GetFlag(RouteOptionValue, 6);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 6);
        }
        public bool InverseColorSignals
        {
            get => BitHelper.GetFlag(RouteOptionValue, 7);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 7);
        }
        public bool Exchange1Row
        {
            get => BitHelper.GetFlag(RouteOptionValue, 8);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 8);
        }
        public bool Exchange2Row
        {
            get => BitHelper.GetFlag(RouteOptionValue, 9);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 9);
        }
        public bool Exchange4Row
        {
            get => BitHelper.GetFlag(RouteOptionValue, 10);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 10);
        }
        public bool Exchange8Row
        {
            get => BitHelper.GetFlag(RouteOptionValue, 11);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 11);
        }
        public int RowExchanges
        {
            get => BitHelper.GetValue(RouteOptionValue, 8, 4);
            set => BitHelper.SetValue(RouteOptionValue, value, 8, 4);
        }
        public bool DecreaseLineOrderBy1
        {
            get => BitHelper.GetFlag(RouteOptionValue, 12);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 12);
        }
        public bool ControllerAtLeftSide
        {
            get => BitHelper.GetFlag(RouteOptionValue, 13);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 13);
        }
        public bool DecodeLineSignals
        {
            get => BitHelper.GetFlag(RouteOptionValue, 14);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 14);
        }
        public RowDriveMode RowDriveMode
        {
            get => (RowDriveMode)((DecreaseLineOrderBy1 ? 1 : 0) | ((DecodeLineSignals ? 1 : 0) << 1));
            set => (DecreaseLineOrderBy1, DecodeLineSignals) = (BitHelper.GetFlag((int)value, 0), BitHelper.GetFlag((int)value, 1));
        }
        public bool IncreaseOutEnableSignal
        {
            get => BitHelper.GetFlag(RouteOptionValue, 15);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 15);
        }
        public ClockDuty ClockDuty
        {
            get => (ClockDuty)BitHelper.GetValue(RouteOptionValue, 16, 2);
            set => RouteOptionValue = BitHelper.SetValue(RouteOptionValue, (int)value, 16, 2);
        }
        public ColorOrder ColorOrder
        {
            get => (ColorOrder)((ExchangeRedAndGreen ? 1 : 0) | (BitHelper.GetValue(RouteOptionValue, 18, 2) << 1));
            set
            {
                var order = (int)value;
                ExchangeRedAndGreen = (order & 1) != 0;
                RouteOptionValue = BitHelper.SetValue(RouteOptionValue, order >> 1, 18, 2);
            }
        }
        public bool Rotated
        {
            get => BitHelper.GetFlag(RouteOptionValue, 20);
            set => RouteOptionValue = BitHelper.SetFlag(RouteOptionValue, value, 20);
        }
        public ControlRange Scale
        {
            get => (ControlRange)BitHelper.GetValue(RouteOptionValue, 21, 3);
            set => RouteOptionValue = BitHelper.SetValue(RouteOptionValue, (int)value, 21, 3);
        }
        public byte Bright { get; set; }
        public byte TestFlag { get; set; }
        public bool SPITestEnabled
        {
            get => BitHelper.GetFlag(TestFlag, 0);
            set => TestFlag = (byte)BitHelper.SetFlag(TestFlag, value, 0);
        }
        public bool SDRAMTestEnabled
        {
            get => BitHelper.GetFlag(TestFlag, 1);
            set => TestFlag = (byte)BitHelper.SetFlag(TestFlag, value, 1);
        }
        public ControlRange Range { get; set; }
        private readonly uint _unused;
        private ushort _PowerOn;
        public int PowerOnHour
        {
            get => BitHelper.GetValue(_PowerOn, 8, 8);
            set => _PowerOn = (ushort)BitHelper.SetValue(_PowerOn, value, 8, 8);
        }
        public int PowerOnMinute
        {
            get => BitHelper.GetValue(_PowerOn, 0, 8);
            set => _PowerOn = (ushort)BitHelper.SetValue(_PowerOn, value, 0, 8);
        }
        private ushort _PowerOff;
        public int PowerOffHour
        {
            get => BitHelper.GetValue(_PowerOff, 8, 8);
            set => _PowerOff = (ushort)BitHelper.SetValue(_PowerOff, value, 8, 8);
        }
        public int PowerOffMinute
        {
            get => BitHelper.GetValue(_PowerOff, 0, 8);
            set => _PowerOff = (ushort)BitHelper.SetValue(_PowerOff, value, 0, 8);
        }
        public string ScanName
        {
            get => GetStringFromFixedLength(_ScanName);
            set => _ScanName = GetFixedLengthStringWithFlash(value, 64);
        }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        private byte[] _ScanName;
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public ushort[] ColorMax { get; set; }
        public ushort Gama { get; set; }
        public short TemperatureOffset { get; set; }
        public TestPlayType TestPlayType { get; set; }
        public ushort LedWidth { get; set; }
        public ushort LedHeight { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public UartConfig[] ComPara { get; set; }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 13)]
        public ushort[] CheckInfo { get; set; }
        public ushort OtherPara { get; set; }
        public bool IsFullColor
        {
            get => BitHelper.GetFlag(OtherPara, 0);
            set => OtherPara = (ushort)BitHelper.SetFlag(OtherPara, value, 0);
        }
        public bool IsCompactRGBSignals
        {
            get => !BitHelper.GetFlag(OtherPara, 1);
            set => OtherPara = (ushort)BitHelper.SetFlag(OtherPara, !value, 1);
        }
        public bool IsSM16188Mode
        {
            get => BitHelper.GetFlag(OtherPara, 2);
            set => OtherPara = (ushort)BitHelper.SetFlag(OtherPara, value, 2);
        }
        public ushort MaxBrightCount { get; set; }
        public ushort MinBrightCount { get; set; }
        public (int Max, int Min) BrightAmend
        {
            get => (MaxBrightCount, MinBrightCount);
            set
            {
                MaxBrightCount = (ushort)value.Max;
                MinBrightCount = (ushort)value.Min;
            }
        }
        /// <summary> 检测设置 </summary>
        public int CheckConfig { get; set; }
        public CheckColorMask CheckColorMask
        {
            get => (CheckColorMask)BitHelper.GetValue(~CheckConfig, 0, 3);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)~value, 0, 3);
        }
        public AnalogInterfaceMode AnalogInterface0Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 8, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 8, 4);
        }
        public AnalogInterfaceMode AnalogInterface1Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 12, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 12, 4);
        }
        public AnalogInterfaceMode AnalogInterface2Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 16, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 16, 4);
        }
        public AnalogInterfaceMode AnalogInterface3Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 20, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 20, 4);
        }
        public AnalogInterfaceMode AnalogInterface4Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 24, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 24, 4);
        }
        public AnalogInterfaceMode AnalogInterface5Mode
        {
            get => (AnalogInterfaceMode)BitHelper.GetValue(CheckConfig, 28, 4);
            set => CheckConfig = BitHelper.SetValue(CheckConfig, (int)value, 28, 4);
        }
        public CardType CardType { get; set; }
        public byte AutoBrightMaxLevel { get; set; }
        public byte AutoBrightMinLevel { get; set; }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 69)]
        private readonly byte[] _unused2;
        public byte ModuleWidth { get; set; }
        public byte ModuleHeight { get; set; }
        public ushort FooterID { get; set; }

        public bool IsValid => FooterID == FooterIdentifier;

        public static LEDConfig CreateInstance() => InnerDeserialize(InitFlashDataBlock(SizeConst));

        public byte[] Serialize() => this.ToBytes();
        public static LEDConfig InnerDeserialize(byte[] data, int offset = 0) => data.ToStruct<LEDConfig>(offset);
        public static LEDConfig Deserialize(byte[] data, int offset = 0)
        {
            var cfg = InnerDeserialize(data, offset);
            return cfg.IsValid ? cfg : throw new ArgumentException();
        }
    }
}
