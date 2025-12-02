using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Number;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [Endian(DefaultEndian)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct CheckInfo
    {
        public const int SizeConst = 76;

        static CheckInfo() => Debug.Assert(Marshal.SizeOf<CheckInfo>() == SizeConst);

        public static CheckInfo Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<CheckInfo>(offset);

        public WORDBool DecreaseLineOrderBy1 { get; }

        public WORDBool DecodeLineSignals { get; }

        public WORDBool IsFullColor { get; }

        public WORDBool IsCompactColorSignals { get; }

        public ushort Width { get; }
        public ushort Height { get; }

        public ControlRange Range => (ControlRange)_Range;
        private readonly ushort _Range;

        public WORDBool IsCheckEnabled { get; }

        /// <summary>
        /// 0: 取前连续的60线<br/>
        /// 1: 取30舍2取30舍2
        /// </summary>
        public WORDBool ChangeSignalsOrder { get; }

        public WORDBool UseJS2 { get; }

        public ushort ScanType { get; }
        public ushort LinesPerGroup { get; }
        public ushort Mode { get; }
        public ushort BitsPerGroup { get; }
        public ushort DotsPerLine { get; }

        public ushort[] SignalMasks => _SignalMasks;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        private readonly ushort[] _SignalMasks;

        public ushort JS2_Pha5 { get; }

        public byte[] LineOrder => _LineOrder;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
        private readonly byte[] _LineOrder;

        public WORDBool IsMainCardFound { get; }

        public WORDBool IsLEDConnected { get; }

        public ushort FaultDotsCount { get; }

        private readonly WORDBool bDisableCheck;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        private readonly uint[] MaskJS;
        /// <summary> 屏蔽颜色 </summary>
        public CheckColorMask ColorMask { get; }

        public WORDBool IsCheckOK { get; }
    }

    [Serializable]
    [Endian(DefaultEndian)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct FaultDotData
    {
        public const int SizeConst = 8;

        static FaultDotData() => Debug.Assert(Marshal.SizeOf<FaultDotData>() == SizeConst);

        public static FaultDotData Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<FaultDotData>(offset);

        /// <summary> 在扫描行中的索引 </summary>
        public ushort RouteIndex { get; }
        private ushort _Line;
        /// <summary> 所在行物理上是第几扫描行 </summary>
        public int Line { get => _Line; set => _Line = (ushort)value; }
        /// <summary> 未故障颜色 </summary>
        public CheckColorMask Color { get; set; }
        public bool IsRedFault => !Color.HasFlag(CheckColorMask.Red);
        public bool IsGreenFault => !Color.HasFlag(CheckColorMask.Green);
        public bool IsBlueFault => !Color.HasFlag(CheckColorMask.Blue);
        /// <summary> 故障次数 </summary>
        public ushort FaultTimes { get; }
    }

    public readonly struct FaultDotInfo
    {
        public FaultDotData Data { get; }
        public Point Point { get; }
        public bool IsRedFault => Data.IsRedFault;
        public bool IsGreenFault => Data.IsGreenFault;
        public bool IsBlueFault => Data.IsBlueFault;

        public static readonly CheckColorMask[] OriginalColorOrder = new CheckColorMask[] { CheckColorMask.Red, CheckColorMask.Green, CheckColorMask.Blue };
        public static readonly IReadOnlyDictionary<char, CheckColorMask> ColorDic = new Dictionary<char, CheckColorMask>()
        {
            { 'R', CheckColorMask.Red },
            { 'r', CheckColorMask.Red },
            { 'G', CheckColorMask.Green },
            { 'g', CheckColorMask.Green },
            { 'B', CheckColorMask.Blue },
            { 'b', CheckColorMask.Blue },
        };

        public FaultDotInfo(FaultDotData data, RouteInfo info, ushort totalWidth, ColorOrder colorOrder = ColorOrder.RGB)
        {
            // 交换大于数据组高度的行，即交换数据组
            data.Line ^= ((int)info.ExtraOptions >> 8) & 0xF & ~(info.LineBreakCount - 1);

            // 交换颜色
            var order = colorOrder.ToString();
            var color = data.Color;
            data.Color = CheckColorMask.None;
            for (var i = 0; i < 3; i++)
                if (color.HasFlag(OriginalColorOrder[i]))
                    data.Color |= ColorDic[order[i]];

            Data = data;

            var rowCells = totalWidth * info.LEDHeight;
            var mCols = (rowCells + info.ScanCells - 1) / info.ScanCells;
            var revIndex = rowCells - Data.RouteIndex - 1;
            var mColCount = revIndex / info.ScanCells;
            var celli = revIndex % info.ScanCells;
            var pt = info.Points[celli];
            pt.Y = info.LEDHeight - pt.Y - 1;
            pt.X += (mCols - mColCount - 1) * info.LEDWidth;
            pt.Y += Data.Line % info.ScanType + Data.Line / info.LEDHeight * info.LEDHeight;
            pt.X = totalWidth - pt.X - 1;
            Point = pt;
        }
        public FaultDotInfo(FaultDotData data, RouteInfo rInfo, LEDConfig ledcfg) : this(data, rInfo, ledcfg.LedWidth, ledcfg.ColorOrder) { }
    }

    public class PowerDotCheckInfo
    {
        public CheckInfo Info { get; }

        public bool IsMainCardFound => Info.IsMainCardFound;
        public bool IsLEDConnected => Info.IsLEDConnected;
        public bool IsCheckOK => IsMainCardFound && IsLEDConnected && Info.IsCheckOK;

        public IPAddress MainCardIPAddress => NetConfig.NTPServerIP;

        public LEDConfig LEDConfig { get; }
        public NetConfig NetConfig { get; }
        public RouteInfo RouteInfo { get; }
        public FaultDotInfo[] FaultDots { get; }

        public PowerDotCheckInfo(CheckInfo info, LEDConfig ledcfg, NetConfig netcfg, RouteInfo routeInfo, FaultDotInfo[] faultDots)
        {
            Info = info;
            LEDConfig = ledcfg;
            NetConfig = netcfg;
            RouteInfo = routeInfo;
            FaultDots = faultDots;
        }
    }

    [Flags]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PowerDotCheckInfoParts
    {
        None = 0,
        CheckInfo = 1 << 0,
        LEDConfig = 1 << 1,
        NetConfig = 1 << 2,
        Route1stPart = 1 << 3,
        Route2ndPart = 1 << 4,
        FaultDots = 1 << 5,
        All = -1
    }

}
