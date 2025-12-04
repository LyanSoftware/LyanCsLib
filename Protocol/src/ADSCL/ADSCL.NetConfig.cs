using System.Diagnostics;
using System.Runtime.InteropServices;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common;
using System.ComponentModel;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using Lytec.Common.Number;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TCPProtocol : byte
        {
            /// <summary> SCL/ADSCL兼容协议 </summary>
            ADSCL = 0,
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

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(Endian.Little)]
        public struct IPv4Address : ICloneable<IPv4Address>, IEquatable<IPv4Address>
        {
            public const int SizeConst = 4;
            static IPv4Address() => Debug.Assert(Marshal.SizeOf<IPv4Address>() == SizeConst);

            public IPAddress Address
            {
                get => new IPAddress(_Address.ToBytes(Endian.Big));
                set => _Address = value.ToInt(Endian.Big);
            }
            [Endian(Endian.Little)]
            private int _Address;

            public IPv4Address(IPAddress addr) : this() => Address = addr;

            public static implicit operator IPAddress(IPv4Address addr) => addr.Address;
            public static implicit operator IPv4Address(IPAddress addr) => new IPv4Address(addr);

            public static bool operator ==(IPv4Address left, IPv4Address right) => left.Equals(right);

            public static bool operator !=(IPv4Address left, IPv4Address right) => !(left == right);

            public override string ToString() => Address.ToString();

            public IPv4Address Clone() => new IPv4Address(Address);

            object ICloneable.Clone() => Clone();

            public override bool Equals(object? obj) => obj is IPv4Address address && Equals(address);

            public bool Equals(IPv4Address other) => _Address == other._Address;

            public override int GetHashCode() => -906666570 + _Address.GetHashCode();
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum AuthMode : byte
        {
            [Description("未使用")]
            Unused = 0xff,
            [Description("仅使用IP限制")]
            OnlyAddress = 0x33,
            [Description("仅使用密码限制")]
            OnlyPassword = 0x55,
            [Description("IP和密码限制都使用")]
            AddressAndPassword = 0xaa
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct AuthInfo
        {
            public const int SizeConst = 8;
            static AuthInfo() => Debug.Assert(Marshal.SizeOf<AuthInfo>() == SizeConst);

            public AuthMode Mode { get; set; }
            public byte Level { get; set; }
            public IPv4Address Address { get; set; }
            public ushort Password { get; set; }

            public override string ToString()
            {
                var sb = new StringBuilder();
                sb.Append("[")
                    .Append(Mode)
                    .Append("]");
                switch (Mode)
                {
                    case AuthMode.OnlyAddress:
                        AppendLevel(Level);
                        AppendAddress(Address);
                        break;
                    case AuthMode.OnlyPassword:
                        AppendLevel(Level);
                        AppendPassword(Password);
                        break;
                    case AuthMode.AddressAndPassword:
                        AppendLevel(Level);
                        AppendAddress(Address);
                        AppendPassword(Password);
                        break;
                }
                return sb.ToString();

                void AppendLevel(byte level) => sb.Append("Level: ").Append(level).Append(" /");
                void AppendAddress(IPv4Address address) => sb.Append(" Address: ").Append(address);
                void AppendPassword(ushort password) => sb.Append(" Password: ").Append(password);
            }
        }

        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TimeZone : byte
        {
            UTC = 0,
            [Description("UTC+1")] East1 = 1,
            [Description("UTC+2")] East2 = 2,
            [Description("UTC+3")] East3 = 3,
            [Description("UTC+4")] East4 = 4,
            [Description("UTC+5")] East5 = 5,
            [Description("UTC+6")] East6 = 6,
            [Description("UTC+7")] East7 = 7,
            [Description("UTC+8")] East8 = 8,
            [Description("UTC+9")] East9 = 9,
            [Description("UTC+10")] East10 = 10,
            [Description("UTC+11")] East11 = 11,
            [Description("UTC-1")] West1 = 12,
            [Description("UTC-2")] West2 = 13,
            [Description("UTC-3")] West3 = 14,
            [Description("UTC-4")] West4 = 15,
            [Description("UTC-5")] West5 = 16,
            [Description("UTC-6")] West6 = 17,
            [Description("UTC-7")] West7 = 18,
            [Description("UTC-8")] West8 = 19,
            [Description("UTC-9")] West9 = 20,
            [Description("UTC-10")] West10 = 21,
            [Description("UTC-11")] West11 = 22,
            [Description("UTC-12")] West12 = 23
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Clock : IPackage
        {
            public const int SizeConst = 14;

            static Clock() => Debug.Assert(Marshal.SizeOf<Clock>() == SizeConst);


            [JsonConverter(typeof(StringEnumConverter))]
            public enum Weekdays : ushort
            {
                Sunday = 0,
                Monday = 1,
                Tuesday = 2,
                Wednesday = 3,
                Thursday = 4,
                Friday = 5,
                Saturday = 6
            }

            public ushort Second { get; set; }
            public ushort Minute { get; set; }
            public ushort Hour { get; set; }
            public Weekdays Weekday { get; set; }
            public ushort Day { get; set; }
            public ushort Month { get; set; }
            public ushort Year { get; set; }

            public Clock(DateTime dt)
            {
                Year = (ushort)(dt.Year % 100);
                Month = (ushort)dt.Month;
                Day = (ushort)dt.Day;
                switch (dt.DayOfWeek)
                {
                    default:
                    case DayOfWeek.Sunday: Weekday = Weekdays.Sunday; break;
                    case DayOfWeek.Monday: Weekday = Weekdays.Monday; break;
                    case DayOfWeek.Tuesday: Weekday = Weekdays.Tuesday; break;
                    case DayOfWeek.Wednesday: Weekday = Weekdays.Wednesday; break;
                    case DayOfWeek.Thursday: Weekday = Weekdays.Thursday; break;
                    case DayOfWeek.Friday: Weekday = Weekdays.Friday; break;
                    case DayOfWeek.Saturday: Weekday = Weekdays.Saturday; break;
                }
                Hour = (ushort)dt.Hour;
                Minute = (ushort)dt.Minute;
                Second = (ushort)dt.Second;
            }

            public byte[] Serialize() => this.ToBytes();

            public static Clock Deserialize(byte[] bytes, int offset = 0)
            => bytes.ToStruct<Clock>(offset);
        }

        public enum VerifyByte : byte
        {
            Unused = 0,
            OK = 1,
            Unset = 0xff,
        }

        public enum WifiMode
        {
            AP = 0,
            STA = 1,
            STA_DHCP = 2,
            Disabled = 3,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct WifiConfig
        {
            public const int SizeConst = 116;
            public const int APMinChannel = 1;
            public const int APMaxChannel = 13;
            public const int MaxConnections = 4;
            public const int SSIDMaxLength = 32;
            public const int PasswordMaxLength = 64;

            static WifiConfig() => Debug.Assert(Marshal.SizeOf<WifiConfig>() == SizeConst);

            public ushort OptionBits { get; set; }
            public WifiMode Mode
            {
                get => (WifiMode)BitHelper.GetValue(OptionBits, 0, 2);
                set => OptionBits = (ushort)BitHelper.SetValue(OptionBits, (int)value, 0, 2);
            }
            public int APChannel
            {
                get => (BitHelper.GetValue(OptionBits, 2, 4) + 1).LimitToRange(APMinChannel, APMaxChannel);
                set => OptionBits = (ushort)BitHelper.SetValue(OptionBits, (value - 1).LimitToRange(APMinChannel, APMaxChannel), 2, 4);
            }
            public bool UseWifiNTP
            {
                get => BitHelper.GetFlag(OptionBits, 2);
                set => OptionBits = (ushort)BitHelper.SetFlag(OptionBits, value, 2);
            }
            public int MaxConnection
            {
                get => BitHelper.GetValue(OptionBits, 7, 2) + 1;
                set => OptionBits = (ushort)BitHelper.SetValue(OptionBits, Math.Max(0, value - 1), 7, 2);
            }
            public bool AutoDisableWhenIdle
            {
                get => BitHelper.GetFlag(OptionBits, 9);
                set => OptionBits = (ushort)BitHelper.SetFlag(OptionBits, value, 9);
            }
            public byte AutoDisableIdleMinutes { get; set; }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            private readonly byte[] unused;
            public string SSID
            {
                get => FromFixedLengthString(SSIDBytes);
                set => SSIDBytes = ToFixedLengthString(value, SSIDMaxLength + 1);
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = SSIDMaxLength + 1)]
            public byte[] SSIDBytes;
            public string Password
            {
                get => FromFixedLengthString(PasswordBytes);
                set => PasswordBytes = ToFixedLengthString(value, PasswordMaxLength + 1);
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = PasswordMaxLength + 1)]
            public byte[] PasswordBytes;
            public IPv4Address IP { get; set; }
            public IPv4Address SubnetMask { get; set; }
            public IPv4Address Gateway { get; set; }

            public WifiConfig()
            {
                unused = new byte[3];
                SSIDBytes = new byte[SSIDMaxLength + 1];
                PasswordBytes = new byte[PasswordMaxLength + 1];
            }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct NTPServerAddress
        {
            public const int SizeConst = NetConfig.NTPServerAddressMaxLength;
            static NTPServerAddress() => Debug.Assert(Marshal.SizeOf<NTPServerAddress>() == SizeConst);
            public string Address
            {
                get => FromFixedLengthString(Bytes);
                set => Bytes = ToFixedLengthString(value, SizeConst);
            }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = SizeConst)]
            public byte[] Bytes { get; set; }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct HeartbeatConfig
        {
            public const int SizeConst = 45;
            static HeartbeatConfig() => Debug.Assert(Marshal.SizeOf<HeartbeatConfig>() == SizeConst);

            public byte PeriodMinutes { get; set; }
            public ushort ServerPort { get; set; }
            public string ServerAddress
            {
                get => FromFixedLengthString(ServerAddressBytes);
                set => ServerAddressBytes = ToFixedLengthString(value, 42);
            }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 42)]
            public byte[] ServerAddressBytes { get; set; }
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public class NetConfig : IPackage
        {
            public const int SizeConst = 768;
            public const int ServerAddrMaxLength = 42;
            public const int NTPServerAddressCount = 4;
            public const int NTPServerAddressMaxLength = 24;
            public const int TCPServerAddressMaxLength = 64;

            static NetConfig() => Debug.Assert(Marshal.SizeOf<NetConfig>() == SizeConst);

            public MacAddress MacAddress { get; set; }
            public VerifyByte MacAddressVerify { get; set; }
            public byte AddrCode { get; set; }
            public string CardTypeStr
            {
                get => FromFixedLengthString(CardTypeStrBytes);
                set => CardTypeStrBytes = ToFixedLengthString(value, 8, false);
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            private byte[] CardTypeStrBytes = new byte[8];
            public string Name
            {
                get => FromFixedLengthString(NameBytes);
                set => NameBytes = ToFixedLengthString(value, NameSize);
            }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = NameSize)]
            private byte[] NameBytes = new byte[NameSize];
            public IPv4Address IP { get; set; }
            public IPv4Address SubnetMask { get; set; }
            public IPv4Address Gateway { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public UartConfig[] ComPara { get; set; } = new UartConfig[2];

            public WifiConfig Wifi { get; set; }

            public IPv4Address LocalNTPServer { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = NTPServerAddressCount)]
            public NTPServerAddress[] NTPServerAddresses { get; set; } = new NTPServerAddress[NTPServerAddressCount];
            private byte _unused;
            public HeartbeatConfig Heartbeat { get; set; }

            public string Password
            {
                get => FromFixedLengthString(PasswordBytes);
                set => PasswordBytes = ToFixedLengthString(value, 12);
            }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] PasswordBytes { get; set; } = new byte[12];
            public ushort PasswordCRC { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public AuthInfo[] AuthInfos { get; set; } = new AuthInfo[16];

            public ushort UDPPort { get; set; }
            public ushort TCPPort { get; set; }
            public ushort JT3000TCPPort { get; set; }
            public DiskDriver JT3000DefaultDisk { get; set; }
            public TCPProtocol TCPProtocol { get; set; }

            public ushort OptionBits { get; set; }
            public bool IsDHCPEnabled
            {
                get => BitHelper.GetFlag(OptionBits, 0);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 0);
            }

            public bool TCPServerMode
            {
                get => BitHelper.GetFlag(OptionBits, 1);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 1);
            }

            public string TCPServerAddress
            {
                get => FromFixedLengthString(TCPServerAddressBytes);
                set => TCPServerAddressBytes = ToFixedLengthString(value, TCPServerAddressMaxLength);
            }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = TCPServerAddressMaxLength)]
            public byte[] TCPServerAddressBytes { get; set; } = new byte[TCPServerAddressMaxLength];
            public short TimeZoneOffsetMinutes { get; set; }

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 210)]
            private readonly byte[] unused = new byte[210];
            public ushort Valid { get; set; }
            public bool IsValid => Valid == 0xAA55;

            public byte[] Serialize() => this.ToBytes();

            public static NetConfig? Deserialize(byte[] bytes, int offset = 0)
            => bytes.ToStruct<NetConfig>(offset) is NetConfig cfg && cfg.IsValid ? cfg : null;
        }

    }
}
