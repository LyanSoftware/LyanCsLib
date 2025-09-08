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

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IPv4Address
        {
            public const int SizeConst = 4;

            public IPAddress Address
            {
                get => new IPAddress((uint)_Address);
                set => _Address = value.ToInt(DefaultEndian); // 数值已经按大端存储了，此时转换需要按默认字节序转
            }
            [Endian(Endian.Big)]
            private int _Address;

            public static readonly IPv4Address Invalid = InitFlashDataBlock(SizeConst).ToStruct<IPv4Address>();

            public IPv4Address(IPAddress addr) : this() => Address = addr;

            public static implicit operator IPAddress(IPv4Address addr) => addr.Address;
            public static implicit operator IPv4Address(IPAddress addr) => new IPv4Address(addr);

            public override string ToString() => Address.ToString();
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct IPv4AddressWithValid
        {
            public const int SizeConst = 5;

            static IPv4AddressWithValid() => Debug.Assert(SizeConst == Marshal.SizeOf<IPv4AddressWithValid>());

            public static readonly IPv4AddressWithValid Invalid = InitFlashDataBlock(SizeConst).ToStruct<IPv4AddressWithValid>();

            public IPv4Address Address { get; set; }
            public bool IsValid => Valid != 0xff;
            private readonly byte Valid;

            public IPv4AddressWithValid(IPv4Address addr)
            {
                Address = addr;
                Valid = 0;
            }

            public IPv4AddressWithValid(IPAddress addr) : this(new IPv4Address(addr)) { }

            public static implicit operator IPv4Address(IPv4AddressWithValid addr) => addr.IsValid ? addr.Address : throw new ArgumentException();
            public static implicit operator IPv4AddressWithValid(IPv4Address addr) => new IPv4AddressWithValid(addr);
            public static implicit operator IPAddress(IPv4AddressWithValid addr) => (IPv4Address)addr;
            public static implicit operator IPv4AddressWithValid(IPAddress addr) => new IPv4AddressWithValid(addr);

            public override string ToString() => IsValid ? Address.ToString() : "Invalid";
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct MacAddressPack
        {
            public const int SizeConst = 8;

            static MacAddressPack() => Debug.Assert(Marshal.SizeOf<MacAddressPack>() == SizeConst);

            public MacAddress Mac { get; set; }
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            private readonly byte[] Unused;

            public MacAddressPack(MacAddress mac)
            {
                Mac = mac;
                Unused = InitFlashDataBlock(2);
            }

            public override string ToString() => Mac.ToString();

            public static implicit operator MacAddress(MacAddressPack mac) => mac.Mac;
            public static implicit operator MacAddressPack(MacAddress mac) => new MacAddressPack(mac);
        }

#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
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
        public struct AuthInfo
        {
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
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct FAT16Date
        {
            public const int SizeConst = 2;
            static FAT16Date() => Debug.Assert(Marshal.SizeOf<FAT16Date>() == SizeConst);

            public ushort Data { get; set; }

            public int Day
            {
                get => BitHelper.GetValue(Data, 0, 5);
                set => Data = (ushort)BitHelper.SetValue(Data, value, 0, 5);
            }

            public int Month
            {
                get => BitHelper.GetValue(Data, 5, 4);
                set => Data = (ushort)BitHelper.SetValue(Data, value, 5, 4);
            }

            public int Year
            {
                get => BitHelper.GetValue(Data, 9, 7) + 1980;
                set => Data = (ushort)BitHelper.SetValue(Data, value - 1980, 9, 7);
            }

            public FAT16Date(DateTime date) : this() => (Year, Month, Day) = (date.Year, date.Month, date.Day);

            public override string ToString() => ((DateTime)this).ToString();

            public static implicit operator DateTime(FAT16Date date) => new DateTime(date.Year, date.Month, date.Day);
            public static implicit operator FAT16Date(DateTime date) => new FAT16Date(date);
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct FAT16Time
        {
            public const int SizeConst = 2;
            static FAT16Time() => Debug.Assert(Marshal.SizeOf<FAT16Time>() == SizeConst);

            public ushort Data { get; set; }

            public int Second
            {
                get => BitHelper.GetValue(Data, 0, 5) * 2;
                set => Data = (ushort)BitHelper.SetValue(Data, value / 2, 0, 5);
            }

            public int Minute
            {
                get => BitHelper.GetValue(Data, 5, 6);
                set => Data = (ushort)BitHelper.SetValue(Data, value, 5, 6);
            }

            public int Hour
            {
                get => BitHelper.GetValue(Data, 11, 5);
                set => Data = (ushort)BitHelper.SetValue(Data, value, 11, 5);
            }

            public FAT16Time(DateTime time) : this() => (Hour, Minute, Second) = (time.Hour, time.Minute, time.Second);

            public override string ToString() => ((DateTime)this).ToString();

            public static implicit operator DateTime(FAT16Time time) => new DateTime(0, 0, 0, time.Hour, time.Minute, time.Second);
            public static implicit operator FAT16Time(DateTime time) => new FAT16Time(time);
        }

        [Serializable]
        [Endian(DefaultEndian)]
        public enum FAT16ItemType : byte
        {
            File = 0x00,
            Dir = 0x10,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct FAT16ItemInfo : IPackage
        {
            public const int SizeConst = 32;
            static FAT16ItemInfo() => Debug.Assert(Marshal.SizeOf<FAT16ItemInfo>() == SizeConst);

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] NameBytes { get; set; }
            public string Name
            {
                get => DefaultEncode.GetString(NameBytes.Take(NameMaxLength).Reverse().SkipWhile(c => c == (byte)' ').Reverse().ToArray());
                set => NameBytes = DefaultEncode.GetBytes(value)
                    .Take(NameMaxLength)
                    .Concat(Enumerable.Repeat<byte>(0, NameMaxLength))
                    .Take(NameMaxLength)
                    .ToArray();
            }
            public int NameMaxLength => Type == FAT16ItemType.Dir ? 3 : 8;
            public int ExtMaxLength => Type == FAT16ItemType.Dir ? 0 : 3;
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
            public byte[] ExtBytes { get; set; }
            public string Ext
            {
                get => DefaultEncode.GetString(ExtBytes.Take(ExtMaxLength).Reverse().SkipWhile(c => c == (byte)' ').Reverse().ToArray());
                set => ExtBytes = DefaultEncode.GetBytes(value)
                    .Take(ExtMaxLength)
                    .Concat(Enumerable.Repeat<byte>(0, ExtMaxLength))
                    .Take(ExtMaxLength)
                    .ToArray();
            }
            public FAT16ItemType Type { get; set; }

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
            private readonly byte[] _Unused;

            public FAT16Time CreateTime { get; set; }
            public FAT16Date CreateDate { get; set; }

            private readonly ushort _Unused2;

            public uint FileSize { get; set; }

            public byte[] Serialize() => this.ToBytes();
            public static FAT16ItemInfo Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<FAT16ItemInfo>(offset, DefaultEndian);
            public static FAT16ItemInfo[] DeserializeAll(byte[] bytes, int offset = 0) => bytes.ToStruct<FAT16ItemInfo[]>(offset, DefaultEndian);
        }

        [Serializable]
#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
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
        [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = DefaultCharSet)]
        [Endian(DefaultEndian)]
        public struct MacConfig : IPackage
        {
            public const int SizeConst = 256;

            static MacConfig() => Debug.Assert(Marshal.SizeOf<MacConfig>() == SizeConst);


            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
            public MacAddressPack[] MacPacks { get; set; }

            [field: MarshalAs(UnmanagedType.ByValTStr, SizeConst = 8)]
            public string CardTypeString { get; set; }

            public MacAddress Mac => MacPacks[0];
            public MacAddress MacAddress => MacPacks[0];

            public static MacConfig CreateInstance() => Deserialize(InitFlashDataBlock(SizeConst));

            public byte[] Serialize() => this.ToBytes();

            public static MacConfig Deserialize(byte[] bytes, int offset = 0)
            => bytes.ToStruct<MacConfig>(offset);
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Clock : IPackage
        {
            public const int SizeConst = 14;

            static Clock() => Debug.Assert(Marshal.SizeOf<Clock>() == SizeConst);


#if USE_NEWTONSOFT_JSON
            [JsonConverter(typeof(StringEnumConverter))]
#endif
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

        [Serializable]
        [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeConst, CharSet = DefaultCharSet)]
        [Endian(DefaultEndian)]
        public struct NetConfig : IPackage
        {
            public const int SizeConst = 256;
            public const int ServerAddrMaxLength = 42;

            static NetConfig() => Debug.Assert(Marshal.SizeOf<NetConfig>() == SizeConst);


            public const uint FooterIdentifier = 0xaa55ffff;

            [field: FieldOffset(0)]
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = SizeConst)]
            public byte[] Data { get; set; }

            public string Name
            {
                get => GetStringFromFixedLength(Data.Take(NameSize).ToArray());
                set => Array.Copy(GetFixedLengthStringWithFlash(value, NameSize), Data, NameSize);
            }
            [field: FieldOffset(16)]
            public IPv4AddressWithValid IP { get; set; }
            [field: FieldOffset(21)]
            public IPv4AddressWithValid NetMask { get; set; }
            [field: FieldOffset(26)]
            public IPv4AddressWithValid GateWay { get; set; }
            [field: FieldOffset(31)]
            public byte AddrCode { get; set; }
            [field: FieldOffset(32)]
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public AuthInfo[] AuthInfos { get; set; }
            [field: FieldOffset(160)]
            public IPv4AddressWithValid NTPServerIP { get; set; }
            [field: FieldOffset(165)]
            public TimeZone TimeZone { get; set; }
            public string ServerAddr
            {
                get => GetStringFromFixedLength(Data.Skip(166).Take(ServerAddrMaxLength).ToArray());
                set
                {
                    var buf = GetFixedLengthStringWithFlash(value, ServerAddrMaxLength);
                    Array.Copy(buf, 0, Data, 166, Math.Min(buf.Length, Data.Length - 166));
                }
            }
            [field: FieldOffset(208)]
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 12)]
            public byte[] ConfigPassword { get; set; }
            [field: FieldOffset(220)]
            public ushort ConfigPasswordCRC { get; set; }
            [field: FieldOffset(222)]
            public IPv4AddressWithValid ServerIP { get; set; }
            [field: FieldOffset(227)]
            public byte HeartbeatPeriod { get; set; }
            [field: FieldOffset(228)]
            public ushort HeartbeatPort { get; set; }
            [field: FieldOffset(230)]
            public IPv4AddressWithValid DNSServer { get; set; }
            [field: FieldOffset(230)]
            public ushort UDPPort { get; set; }
            [field: FieldOffset(232)]
            public TCPProtocol TCPProtocol { get; set; }
            [field: FieldOffset(233)]
            private readonly ushort _unused;
            [field: FieldOffset(235)]
            public byte DHCPCfg { get; set; }
            public bool IsDHCPEnabled
            {
                get => BitHelper.GetFlag(~DHCPCfg, 0);
                set => DHCPCfg = (byte)BitHelper.SetFlag(DHCPCfg, !value, 0);
            }
            [field: FieldOffset(236)]
            public ushort TCPPort { get; set; }
            [field: FieldOffset(238)]
            public Clock Clock { get; set; }
            [field: FieldOffset(252)]
            public uint FooterID { get; set; }

            public bool IsValid => FooterID == FooterIdentifier && IP.IsValid;


            public static NetConfig CreateInstance() => InnerDeserialize(InitFlashDataBlock(SizeConst));

            public byte[] Serialize() => this.ToBytes();

            private static NetConfig InnerDeserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<NetConfig>(offset);

            public static NetConfig Deserialize(byte[] bytes, int offset = 0)
            {
                var cfg = InnerDeserialize(bytes, offset);
                return cfg.IsValid && cfg.IP.IsValid ? cfg : throw new ArgumentException();
            }
        }

    }
}
