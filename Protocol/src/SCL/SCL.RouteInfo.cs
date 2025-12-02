namespace Lytec.Protocol;

public static partial class SCL
{
    /// <summary>LED走线信息</summary>
    public class RouteInfo
    {
        /// <summary> 路由数据bit数 </summary>
        public const int DataBits = 10; // 1 << 10 = 1024
        /// <summary> 路由数据mask </summary>
        public const int DataMask = (1 << DataBits) - 1;

        public const int DefaultLEDWidth = 64;
        public const int DefaultLEDHeight = 16;

        /// <summary> 无法在路由表内部处理的路由选项 </summary>
        public RouteOption ExtraOptions { get; }

        /// <summary> 最小单元的宽度 </summary>
        public int LEDWidth { get; } = DefaultLEDWidth;
        /// <summary> 最小单元的单组数据高度 </summary>
        public int LEDHeight { get; } = DefaultLEDHeight;
        /// <summary> 最小单元的单组数据总点数 </summary>
        public int CellCount { get; } = DefaultLEDWidth * DefaultLEDHeight;

        /// <summary> 路由数据 </summary>
        public RouteData Data { get; }

        public Point[] Points { get; }
        public int[,] Table { get; }

        public Point this[int index] => Points[index];
        public int this[int x, int y] => Table[x, y];

        /// <summary> 扫描类型，仅支持 1/1（静态）、1/2、1/4、1/8、1/16 </summary>
        public int ScanType { get; }
        /// <summary> 需要将路由数据的多少bit用作折行行号 </summary>
        public int ScanMode { get; }
        /// <summary> 每组数据有多少物理行 </summary>
        public int ScanPer { get; }
        /// <summary> 每扫描实际有多少物理行 </summary>
        public int ScanRows { get; }
        /// <summary> 每扫描行实际有多少列（点） </summary>
        public int ScanCells { get; }
        /// <summary> 路由数据中，多少bit是列数据 </summary>
        public int ColumnBits { get; }
        /// <summary> 列数据mask </summary>
        public int ColumnMask { get; }
        /// <summary> 扫描行折行次数 </summary>
        public int LineBreakCount { get; }
        /// <summary> 扫描行折行后的行宽 </summary>
        public int LineBreakOffset { get; }

        public RouteInfo(int scanType, int scanPer, RouteData data, RouteOption options = RouteOption.None, byte inverseEvenRowColumnWidth = 0)
        {
            ExtraOptions = options;
            ScanType = scanType;
            ScanPer = scanPer;
            Data = data;

            LEDHeight = Math.Min(ScanPer, LEDHeight);
            if (ScanType >= ScanPer)
                inverseEvenRowColumnWidth = 0;

            ScanMode = (int)Math.Log(LEDHeight / ScanType, 2);
            ScanRows = LEDHeight / ScanType;
            ScanCells = ScanRows * LEDWidth;
            LineBreakCount = 1 << ScanMode;
            LineBreakOffset = RouteData.Length / LineBreakCount;
            ColumnBits = DataBits - ScanMode;
            ColumnMask = (1 << ColumnBits) - 1;

            // 解析原始路由表数据
            Table = new int[LineBreakOffset, LineBreakCount];
            for (var i = 0; i < RouteData.Length; i++)
            {
                var x = Data[i] & ColumnMask;
                var y = (Data[i] & DataMask) >> ColumnBits;
                Table[x, y] = i;
            }

            // 交换行列
            var exchangeCols = (int)ExtraOptions & 0x0F;
            var exchangeRows = ((int)ExtraOptions >> 8) & 0xF;
            void swap(int x1, int y1, int x2, int y2)
            => (Table[x2, y2], Table[x1, y1]) = (Table[x1, y1], Table[x2, y2]);
            for (var i = 0; i < 4; i++)
            {
                var j = 1 << (i + 1);
                var offset = j / 2;
                // 交换列
                var exc = exchangeCols & (1 << i);
                if (exc < LineBreakOffset && exc != 0)
                {
                    for (var x = 0; x < LineBreakOffset; x += j)
                        for (var y = 0; y < LineBreakCount; y++)
                            for (var k = 0; k < offset; k++)
                                swap(x + k, y, x + k + offset, y);
                    ExtraOptions &= (RouteOption)~exc;
                }
                // 交换行
                exc = exchangeRows & (1 << i);
                if (exc < LineBreakCount && exc != 0)
                {
                    for (var y = 0; y < LineBreakCount; y += j)
                        for (var x = 0; x < LineBreakOffset; x++)
                            for (var k = 0; k < offset; k++)
                                swap(x, y + k, x, y + k + offset);
                    ExtraOptions &= (RouteOption)~(exc << 8);
                }
            }
            if (options.HasFlag(RouteOption.InverseEvenAndOddLineGroups) && inverseEvenRowColumnWidth > 1)
            {
                // 奇偶行组列反向
                var offset = inverseEvenRowColumnWidth / 2;
                for (var y = 1; y < LineBreakCount; y += 2)
                    for (var x = 0; x < LineBreakOffset; x += inverseEvenRowColumnWidth)
                        for (var i = 0; i < offset; i++)
                            swap(x + i, y, x + inverseEvenRowColumnWidth - 1 - i, y);
                ExtraOptions &= ~RouteOption.InverseEvenAndOddLineGroups;
            }
            // 填入Points
            Points = new Point[RouteData.Length];
            for (var x = 0; x < LineBreakOffset; x++)
                for (var y = 0; y < LineBreakCount; y++)
                    Points[Table[x, y]] = new Point(x, y);
        }

        public RouteInfo(string scanName, RouteData data, RouteOption options = RouteOption.None, byte inverseEvenRowColumnWidth = 0)
            : this(byte.Parse(scanName.Substring(0, 2)), byte.Parse(scanName.Substring(4, 2)), data, options, inverseEvenRowColumnWidth) { }

        public RouteInfo(LEDConfig conf, RouteData data)
            : this(conf.ScanName, data, conf.RouteOption, (conf.InverseEvenAndOddLineGroups && byte.TryParse(conf.ScanName.Substring(7, 2), out var w)) ? w : (byte)0) { }
    }
}
