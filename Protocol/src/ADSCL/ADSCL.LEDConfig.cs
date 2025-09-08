using System.Diagnostics;
using System.Runtime.InteropServices;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ControlRange : byte
        {
            Range4032x128 = 0,
            Range2048x256 = 1,
            Range1024x512 = 2,
            Range2048x128 = 3,
            Range1024x256 = 4,
            Range512x512 = 5,
        }

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

        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum ClockDuty
        {
            Percent25 = 0,
            Percent50 = 1,
            Percent75 = 2
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct AnalogIOConfig
        {
            public int Data;

            public int Mode
            {
                get => BitHelper.GetValue(Data, 0, 4);
                set => Data = BitHelper.SetValue(Data, value, 0, 4);
            }
        }

        public interface IModuleConfigs : IPackage
        {
            byte Chip { get; set; }
        }

        public enum Module2500Chip : byte
        {
            Normal = 0,
            SM16188 = 1,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2500Configs : IModuleConfigs
        {
            public Module2500Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2500Chip)value; }

            public byte OptionBits { get; set; }

            public bool UseFpgaUart
            {
                get => BitHelper.GetFlag(OptionBits, 0);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 0);
            }

            public bool FpgaUartOutputIsSerial
            {
                get => BitHelper.GetFlag(OptionBits, 1);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 1);
            }

            public bool FpgaUartInputIsSerial
            {
                get => BitHelper.GetFlag(OptionBits, 2);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 2);
            }

            public bool ScreenCopyToFpgaUart
            {
                get => BitHelper.GetFlag(OptionBits, 3);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 3);
            }

            public int FpgaUartBaudrate { get; set; }

            public byte[] Serialize() => this.ToBytes();
        }

        public enum Module2800Chip : byte
        {
            Normal = 0,
            LYAN6039 = Normal,
            SM16389SF = Normal,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2800Configs : IModuleConfigs
        {
            public Module2800Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2800Chip)value; }

            public byte[] Serialize() => this.ToBytes();

        }

        public enum Module2900Chip : byte
        {
            Normal = 0,
            ICND2110 = Normal,
            ICND2112 = 1,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2900Configs : IModuleConfigs
        {
            public Module2900Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2900Chip)value; }

            public byte[] Serialize() => this.ToBytes();
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public class LEDConfig : IPackage
        {
            public const int SizeConst = 256;
            static LEDConfig() => Debug.Assert(Marshal.SizeOf<LEDConfig>() == SizeConst);

            public int OptionBits { get; set; }

            public int ColExchanges
            {
                get => BitHelper.GetValue(OptionBits, 0, 4);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 0, 4);
            }

            public int RowExchanges
            {
                get => BitHelper.GetValue(OptionBits, 4, 4);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 4, 4);
            }

            public bool ReverseEvenRowCols
            {
                get => BitHelper.GetFlag(OptionBits, 8);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 8);
            }

            public bool LineOrderOffsetDec1
            {
                get => BitHelper.GetFlag(OptionBits, 9);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 9);
            }

            public bool IsOnLeftSide
            {
                get => BitHelper.GetFlag(OptionBits, 10);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 10);
            }

            public bool InvertDataSignal
            {
                get => BitHelper.GetFlag(OptionBits, 11);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 11);
            }

            public bool LineDecode
            {
                get => BitHelper.GetFlag(OptionBits, 12);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 12);
            }

            public bool WidenOESignal
            {
                get => BitHelper.GetFlag(OptionBits, 13);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 13);
            }

            public bool IsFullColor
            {
                get => BitHelper.GetFlag(OptionBits, 14);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 14);
            }

            public bool IsCompactColorSignals
            {
                get => BitHelper.GetFlag(OptionBits, 15);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 15);
            }

            public ClockDuty ClockDuty
            {
                get => (ClockDuty)BitHelper.GetValue(OptionBits, 16, 2);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 16, 2);
            }

            public ControlRange Scale
            {
                get => (ControlRange)BitHelper.GetValue(OptionBits, 18, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 18, 3);
            }

            public ControlRange Range
            {
                get => (ControlRange)BitHelper.GetValue(OptionBits, 21, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 21, 3);
            }

            public ColorOrder ColorOrder
            {
                get => (ColorOrder)BitHelper.GetValue(OptionBits, 24, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 24, 3);
            }

            public bool Rotate
            {
                get => BitHelper.GetFlag(OptionBits, 27);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 27);
            }

            public bool UseCheckCard
            {
                get => BitHelper.GetFlag(OptionBits, 28);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 28);
            }

            public bool DotCheckIgnoreR
            {
                get => BitHelper.GetFlag(OptionBits, 29);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 29);
            }

            public bool DotCheckIgnoreG
            {
                get => BitHelper.GetFlag(OptionBits, 30);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 30);
            }

            public bool DotCheckIgnoreB
            {
                get => BitHelper.GetFlag(OptionBits, 31);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 31);
            }

            public int DotCheckColorMask
            {
                get => BitHelper.GetValue(OptionBits, 29, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 29, 3);
            }

            public byte ScanMode { get; set; }

            public byte DataGroupHeight { get; set; }

            public byte ModuleWidth { get; set; }

            public byte ModuleHeight { get; set; }

            public ushort LedWidth { get; set; }

            public ushort LedHeight { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] VINTFreqDiv { get; set; } = new byte[5];

            public CardType CardType { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ModuleConfigsData { get; set; } = new byte[16];
            public IModuleConfigs? ModuleConfigs
            {
                get
                {
                    switch (CardType)
                    {
                        case CardType.ADSCL2500:
                            return ModuleConfigsData.ToStruct<Module2500Configs>();
                        case CardType.ADSCL2800:
                            return ModuleConfigsData.ToStruct<Module2800Configs>();
                        case CardType.ADSCL2900:
                            return ModuleConfigsData.ToStruct<Module2900Configs>();
                        default:
                            return null;
                    }
                }
                set
                {
                    void set(IModuleConfigs cfg) => ModuleConfigsData = cfg.Serialize().Concat(Enumerable.Repeat<byte>(0, 16)).Take(16).ToArray(); ;
                    switch (CardType)
                    {
                        case CardType.ADSCL2500:
                            {
                                if (value is Module2500Configs cfg)
                                    set(cfg);
                                else throw new ArgumentException();
                                break;
                            }
                        case CardType.ADSCL2800:
                            {
                                if (value is Module2800Configs cfg)
                                    set(cfg);
                                else throw new ArgumentException();
                                break;
                            }
                        case CardType.ADSCL2900:
                            {
                                if (value is Module2900Configs cfg)
                                    set(cfg);
                                else throw new ArgumentException();
                                break;
                            }
                        default:
                            throw new NotSupportedException();
                    }
                }
            }

            public byte GammaValue { get; set; }
            public float Gamma { get => GammaValue / 10f; set => GammaValue = (byte)(value * 10); }

            public byte BrightOptions { get; set; }

            public int Bright
            {
                get => BitHelper.GetValue(BrightOptions, 0, 6);
                set => BrightOptions = (byte)BitHelper.SetValue(BrightOptions, value.LimitToRange(0, 63), 0, 6);
            }

            public bool UseAutoBright
            {
                get => BitHelper.GetFlag(BrightOptions, 6);
                set => BrightOptions = (byte)BitHelper.SetFlag(BrightOptions, value, 6);
            }

            public byte AutoBrightMaxLevel { get; set; }

            public byte AutoBrightMinLevel { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] BrightSensorADVTable { get; set; } = new byte[32];

            public ushort PowerOnTimeValue { get; set; }
            public (int Hour, int Minute) PowerOnTime
            {
                get => (PowerOnTimeValue >> 8, PowerOnTimeValue & 0xff);
                set => PowerOnTimeValue = (ushort)((value.Hour.LimitToRange(0, 23) << 8) | value.Minute.LimitToRange(0, 59));
            }

            public ushort PowerOffTimeValue { get; set; }
            public (int Hour, int Minute) PowerOffTime
            {
                get => (PowerOffTimeValue >> 8, PowerOffTimeValue & 0xff);
                set => PowerOffTimeValue = (ushort)((value.Hour.LimitToRange(0, 23) << 8) | value.Minute.LimitToRange(0, 59));
            }

            public short TemperatureOffset { get; set; }

            public TestPlayType TestPlayType { get; set; }

            public ushort SPIHideSecCount { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UartConfig[] ComPara { get; set; } = new UartConfig[3];

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public AnalogIOConfig[] AnalogIOConfigs { get; set; } = new AnalogIOConfig[6];

            public byte StaggerOptions { get; set; }

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 114)]
            private readonly byte[] unused = new byte[114];

            public uint PoTimeout { get; set; }

            public ushort Valid { get; set; }

            public bool IsValid => Valid == 0xAA55;

            public byte[] Serialize() => this.ToBytes();

            public static LEDConfig TryParse(byte[] bytes, int offset = 0) => bytes.ToStruct<LEDConfig>(offset);
        }

    }
}
