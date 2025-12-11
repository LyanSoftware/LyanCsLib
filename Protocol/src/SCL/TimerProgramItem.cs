using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using RGB565Color = Lytec.Protocol.ADSCL.RGB565Color;
using static Lytec.Protocol.SCL.Constants;
using Lytec.Common.Serialization;

namespace Lytec.Protocol
{
    partial class SCL
    {
        public class TimerProgramItem : IPackage
        {
            public enum ItemType : byte
            {
                Year = 1,
                Month = 2,
                Day = 3,
                Weekday = 4,
                Hour = 5,
                Minute = 6,
                Second = 7,
                Temperature = 8,
                Humidity = 9,
                Counter = 10,
                ComData = 11,
                UVLevel = 12,
                UVValue = 13,
                AnalogClock = 14,
                TimedProgressBar = 15,
            }

            public enum TemperatureUnit : byte
            {
                // 摄氏度
                Celsius = 0,
                // 华氏度
                Fahrenheit = 1,
            }

            public interface IItem : IPackage { }

            public enum CounterType
            {
                // 正计日
                CountUp_Day = 0,
                // 倒计日
                CountDown_Day = 1,
                // 倒计时, 数字时钟
                CountDown_DigitClock = 2,
                // 倒计时, 秒
                CountDown_Seconds = 3,
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct CounterConfigs
            {
                public byte Data { get; set; }
                public CounterType Type
                {
                    get => (CounterType)BitHelper.GetValue(Data, 0, 3);
                    set => Data = (byte)BitHelper.SetValue(Data, (int)value, 0, 3);
                }
                // 不显示 时 ,只显示分和秒
                // 此时, BitMax作为分钟数位数使用
                public bool NoHours
                {
                    get => BitHelper.GetFlag(Data, 3);
                    set => Data = (byte)BitHelper.SetFlag(Data, value, 3);
                }
                // 相对计时, 相对节目开始播放的时间计时
                public bool IsRelative
                {
                    get => BitHelper.GetFlag(Data, 4);
                    set => Data = (byte)BitHelper.SetFlag(Data, value, 4);
                }

                public CounterConfigs(byte data) => Data = data;
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct BasicItem : IItem
            {
                public const int SizeConst = 24;
                static BasicItem() => Debug.Assert(Marshal.SizeOf<BasicItem>() == SizeConst);

                public ItemType Type { get; set; }
                public byte Red { get; set; }
                public byte Green { get; set; }
                public byte Blue { get; set; }
                public ushort FontOffset { get; set; }
                public byte BitMax { get; set; }
                public byte SharedByte { get; set; }
                // 串口数据索引,允许在一个版面对同一串口布置多个数据
                public byte Index { get => SharedByte; set => SharedByte = value; }
                public CounterConfigs Counter { get => new CounterConfigs(SharedByte); set => SharedByte = value.Data; }
                // 时区修正值
                public sbyte TimeZoneOffset { get => (sbyte)SharedByte; set => SharedByte = (byte)value; }
                public TemperatureUnit TemperatureUnit { get => (TemperatureUnit)SharedByte; set => SharedByte = (byte)value; }
                public uint Value { get; set; }
                public byte ComPort { get; set; }
                public byte HideZero { get; set; }
                public byte Year { get; set; }
                public byte Month { get; set; }
                public byte Day { get; set; }
                public byte Step { get; set; }
                public ushort YPos { get; set; }
                public ushort XPos { get; set; }
                public ushort dNC1 { get; set; }

                public byte[] Serialize() => this.ToBytes();
                public static BasicItem? Deserialize(byte[] bytes, int offset = 0)
                => bytes.TryToStruct<BasicItem>(out var d, offset) ? d : null;
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct AnalogClockItem : IItem
            {
                public const int SizeConst = 24;
                static AnalogClockItem() => Debug.Assert(Marshal.SizeOf<AnalogClockItem>() == SizeConst);

                public ItemType Type { get; set; }
                // 模拟时钟背景的宽度
                public byte BkWidth { get; set; }
                // 模拟时钟背景的高度
                public byte BkHeight { get; set; }
                // 是否显示秒针标记,0:只有长短指针,1:附加秒指针
                public byte bShowSecond { get; set; }
                // 背景字节数
                public ushort BkByteSize { get; set; }
                private readonly byte aNC0;
                // 时区
                public sbyte aZone { get; set; }
                // 模拟时钟背景在时钟文件中的偏移
                public uint BkOffset { get; set; }
                // 指针图片的宽度
                public byte SWidth { get; set; }
                // 指针图片的高度
                public byte SHeight { get; set; }
                // 显示过程中的分步骤计数,计算机程序必须将其初始化为0
                public byte Step { get; set; }
                // 记录已处理的分或秒数值,计算机程序必须将其初始化为255
                public byte OldValue { get; set; }
                private readonly byte aNC1;
                private readonly byte aNC2;
                // 模拟时钟在时钟版面中的位置
                public ushort aYPosi { get; set; }
                public ushort aXPosi { get; set; }
                // 指针图片字节数
                public ushort SByteSize { get; set; }

                public byte[] Serialize()
                {
                    Type = ItemType.AnalogClock;
                    Step = 0;
                    OldValue = 255;
                    return this.ToBytes();
                }
                public static AnalogClockItem? Deserialize(byte[] bytes, int offset = 0)
                => bytes.TryToStruct<AnalogClockItem>(out var d, offset) ? d : null;
            }

            public enum TimedProgressBarMode
            {
                // 只播放一遍
                Normal = 0,
                // 忽略"进度条全长", 改用节目停留时间作为进度条全长
                UseStaySec = 1,
                // 循环播放
                Loop = 2,
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct TimedProgressBarItem : IItem
            {
                public const int SizeConst = 24;
                static TimedProgressBarItem() => Debug.Assert(Marshal.SizeOf<TimedProgressBarItem>() == SizeConst);

                public ItemType Type { get; set; }
                public byte OptionBits { get; set; }
                public TimedProgressBarMode Mode
                {
                    get => (TimedProgressBarMode)BitHelper.GetValue(OptionBits, 0, 2);
                    set => OptionBits = (byte)BitHelper.SetValue(OptionBits, (int)value, 0, 2);
                }
                // 填充方向是否为逆向
                // false: 正向, 区域初始为初始值, 从起点开始逐渐填充背景色直到全部为背景色
                // true : 逆向, 区域初始为初始值, 从终点开始逐渐填充背景色直到全部为背景色
                public bool IsBackward
                {
                    get => BitHelper.GetFlag(OptionBits, 2);
                    set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 2);
                }
                // 绘制方向是否为垂直
                // false: 水平, 从左往右 (从右往左配置为逆向+反色实现)
                // true : 垂直, 从上往下 (从下往上配置为逆向+反色实现)
                public bool IsVertical
                {
                    get => BitHelper.GetFlag(OptionBits, 3);
                    set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 3);
                }
                // 进度条区域位置和大小
                public ushort XPos { get; set; }
                public ushort YPos { get; set; }
                public ushort Width { get; set; }
                public ushort Height { get; set; }
                // 填充色
                public RGB565Color FillColor { get; set; }
                // 背景色
                public RGB565Color BackColor { get; set; }
                // 进度条全长（ms）
                public uint MaxValueMs { get; set; }
                // 当前值, 会在播放过程中被修改, 上位机给的初始值应该为0
                public ushort Value { get; set; }
                // 计时开始时间, 会在播放过程中被修改, 上位机给的初始值应该为0
                public uint StartTicks { get; set; }

                public byte[] Serialize()
                {
                    Type = ItemType.TimedProgressBar;
                    Value = 0;
                    StartTicks = 0;
                    return this.ToBytes();
                }
                public static TimedProgressBarItem? Deserialize(byte[] bytes, int offset = 0)
                => bytes.TryToStruct<TimedProgressBarItem>(out var d, offset) ? d : null;
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Explicit, Size = SizeConst)]
            public struct Headers : IItem
            {
                public const int SizeConst = 24;

                [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = SizeConst)]
                [field: FieldOffset(0)]
                public byte[] Data { get; set; }

                [field: FieldOffset(0)]
                public BasicItem BasicItem { get; set; }

                [field: FieldOffset(0)]
                public AnalogClockItem AnalogClock { get; set; }

                [field: FieldOffset(0)]
                public TimedProgressBarItem TimedProgressBar { get; set; }

                public byte[] Serialize() => Data.ToArray();
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct FontInfo : IPackage
            {
                public const int SizeConst = 6;
                static FontInfo() => Debug.Assert(Marshal.SizeOf<FontInfo>() == SizeConst);

                public ushort CharHeight { get; set; }
                public ushort CharWidth { get; set; }
                public ushort CharByteCount { get; set; }

                public byte[] Serialize() => this.ToBytes();
                public static FontInfo? Deserialize(byte[] bytes, int offset = 0)
                => bytes.TryToStruct<FontInfo>(out var fi, offset) ? fi : null;
            }

            public enum FileVer : ushort
            {
                RGB565 = 1,
                RG88 = 2,
                RG11 = 3,
                RGBn1111 = 5,
            }

            [Serializable]
            [Endian(DefaultEndian)]
            [StructLayout(LayoutKind.Sequential, Pack = 1)]
            public struct FileHeader : IPackage
            {
                public const int SizeConst = 12;
                static FileHeader() => Debug.Assert(Marshal.SizeOf<FileHeader>() == SizeConst);

                public FileVer FileVer { get; set; }
                public ushort Height { get; set; }
                public ushort Width { get; set; }
                public ushort ItemCount { get; set; }
                public ushort FontCount { get; set; }
                public ushort PicOffset { get; set; }

                public byte[] Serialize() => this.ToBytes();
                public static FileHeader? Deserialize(byte[] bytes, int offset = 0)
                => bytes.TryToStruct<FileHeader>(out var fi, offset) ? fi : null;
            }


            public FileVer Ver { get; set; }
            public ushort Width { get; set; }
            public ushort Height { get; set; }
            public IList<(IItem Item, int FontIndex)> Items { get; set; } = new List<(IItem Item, int FontIndex)>();
            public IList<IDictionary<string, ADSCL.Xmp>> Fonts { get; set; } = new List<IDictionary<string, ADSCL.Xmp>>();
            public ADSCL.Xmp? BgImage { get; set; }

            public static readonly string[] FontChars_Normal = Enumerable.Range(0, 10).Select(i => '0' + i)
                .Append('+')
                .Append('-')
                .Append(' ')
                .Append('.')
                .Concat(Enumerable.Range(0, 26).Select(i => 'A' + i))
                .Select(c => ((char)c).ToString())
                .ToArray();
            
            public static readonly string[] FontChars_Digit = Enumerable.Range(0, 10).Select(i => '0' + i)
                .Append('+')
                .Append('-')
                .Append(' ')
                .Append('.')
                .Select(c => ((char)c).ToString())
                .Concat(Enumerable.Range(0, 10).Select(i => $".{i}"))
                .ToArray();

            public byte[] Serialize()
            {
                if (Items.Count > ushort.MaxValue
                    || Fonts.Count > ushort.MaxValue)
                    throw new InvalidOperationException();
                if (BgImage != null)
                {
                    if (BgImage.Width != Width
                        || BgImage.Height != Height)
                        throw new InvalidDataException();
                }

                foreach (var font in Fonts)
                {
                    var fc1 = font.First().Value;
                    if (!font.All(f => f.Value.Width == fc1.Width && f.Value.Height == fc1.Height))
                        throw new InvalidDataException();
                }
                var fonts = new List<(FontInfo Info, int Offset, byte[] Data)>();
                var fontOffset = FileHeader.SizeConst + Items.Count * BasicItem.SizeConst;
                foreach (var (info, data) in Fonts.Select((f, i) =>
                {
                    var fc1 = f.First().Value;
                    return (
                        Info: new FontInfo()
                        {
                            CharWidth = f.First().Value.Width,
                            CharHeight = f.First().Value.Height,
                            CharByteCount = (ushort)ADSCL.Xmp.GetDataSize(ADSCL.XmpType.R1, fc1.Width, fc1.Height, false),
                        },
                        Data: FontChars_Normal
                            .Select(c => f.TryGetValue(c, out var d) ? d : new ADSCL.Xmp(ADSCL.XmpType.R1, fc1.Width, fc1.Height))
                            .SelectMany(c => c.Serialize(ADSCL.XmpType.R1))
                            .ToArray()
                    );
                }))
                {
                    fonts.Add((info, fontOffset, data));
                    fontOffset += FontInfo.SizeConst + data.Length;
                }

                var head = new FileHeader()
                {
                    FileVer = Ver,
                    Width = Width,
                    Height = Height,
                    ItemCount = (ushort)Items.Count,
                    FontCount = (ushort)fonts.Count,
                    PicOffset = (ushort)fontOffset,
                };

                var buf = new List<byte>();
                buf.AddRange(head.Serialize());
                foreach (var (item, fontIndex) in Items)
                {
                    if (item is BasicItem bi)
                    {
                        bi.FontOffset = (ushort)fonts[fontIndex].Offset;
                        buf.AddRange(bi.Serialize());
                    }
                    else buf.AddRange(item.Serialize());
                }
                foreach (var (info, _, data) in fonts)
                {
                    buf.AddRange(info.Serialize());
                    buf.AddRange(data);
                }
                var bgType = Ver switch
                {
                    FileVer.RGB565 => ADSCL.XmpType.RGB565,
                    FileVer.RG88 => ADSCL.XmpType.RG88,
                    FileVer.RG11 => ADSCL.XmpType.RG11,
                    FileVer.RGBn1111 => ADSCL.XmpType.RGBn1111,
                    _ => throw new NotSupportedException(),
                };
                var bg = BgImage ?? new ADSCL.Xmp(bgType, Width, Height);
                buf.AddRange(bg.Serialize(bgType));

                return buf.ToArray();
            }
        }
    }
}
