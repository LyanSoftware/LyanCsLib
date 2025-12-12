using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using Lytec.Common;
using System.ComponentModel;
using System.Text;
using static Lytec.Protocol.SCL.Constants;
using System.Net;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IPv4Address
    {
        public const int SizeConst = 4;

        public IPAddress Address
        {
            get => new IPAddress(_Address.ToBytes(Endian.Big));
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
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    [Endian(DefaultEndian)]
    public struct MacConfig : IPackage
    {
        public const int SizeConst = 256;

        static MacConfig() => Debug.Assert(Marshal.SizeOf<MacConfig>() == SizeConst);


        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 31)]
        public MacAddressPack[] MacPacks { get; set; }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private byte[] CardTypeStringBytes;
        public string CardTypeString
        {
            get => GetStringFromFixedLength(CardTypeStringBytes);
            set => CardTypeStringBytes = GetFixedLengthStringWithFlash(value, 8, false);
        }

        public MacAddress Mac => MacPacks[0];
        public MacAddress MacAddress => MacPacks[0];

        public bool IsADSCL => CardTypeString switch
        {
            "CHECK25"
            or "CE2500"
            or "CE2600"
            or "CE2610"
            or "CE2620"
            or "ADSL250"
            or "AD2500"
            or "AD2510"
            or "AD2520"
            or "ADSL280"
            or "AD2800"
            or "AD2810"
            or "AD2820"
            or "AD2900"
            or "AD2910"
            or "AD2920"
            => true,
            _ => false,
        };
        
        public bool IsCheck => CardTypeString switch
        {
            "CHECK25"
            or "CE2500"
            or "CE2600"
            or "CE2610"
            or "CE2620"
            => true,
            _ => false,
        };

        public bool IsSCL2008
        {
            get
            {
                if (IsADSCL)
                    return true;
                var str = CardTypeString;
                switch (str)
                {
                    case "":
                    case "SCL2008":
                        return true;
                }
                return str.Length >= 3
                    && str[0] == 0x6C
                    && str[1] == 0x59
                    && str[2] == 0x54;
            }
        }

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

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct MacNetConfig : IPackage
    {
        public const int SizeConst = MacConfig.SizeConst + NetConfig.SizeConst;

        public MacConfig MacConfig { get; set; }

        public NetConfig NetConfig { get; set; }

        public MacNetConfig(in MacConfig mac, in NetConfig net) => (MacConfig, NetConfig) = (mac, net);

        public byte[] Serialize() => this.ToBytes();

        public static MacNetConfig Deserialize(byte[] bytes, int offset = 0)
        {
            var info = bytes.ToStruct<MacNetConfig>(offset);
            return info.NetConfig.IsValid ? info : throw new ArgumentException();
        }
    }

    [Serializable]
    [StructLayout(LayoutKind.Explicit, Pack = 1, Size = SizeConst, CharSet = CharSet.Ansi)]
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
        public TcpProtocol TCPProtocol { get; set; }
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
