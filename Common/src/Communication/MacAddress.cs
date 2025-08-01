using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Net.NetworkInformation;
using System.Reflection;
using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Newtonsoft.Json;

namespace Lytec.Common.Communication
{

    /// <summary>
    /// 表示网络物理地址，可与 <see cref="PhysicalAddress"/> 互相转换
    /// </summary>
    [Endian(Endian.Big)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [TypeConverter(typeof(Converters.StringTypeConverter<MacAddress>))]
    [JsonObject]
    [JsonConverter(typeof(Converters.StringJsonConverter))]
    public struct MacAddress : IEquatable<MacAddress>, IComparable<MacAddress>
    {
        public const int SizeConst = 6;

        static MacAddress() => Debug.Assert(Marshal.SizeOf<MacAddress>() == SizeConst);

        public static readonly MacAddress Empty = new MacAddress();
        public static readonly MacAddress Broadcast = new MacAddress(-1);
        private static readonly Random MacGen = new Random();
        public static MacAddress Random
        {
            get
            {
                var buf = new byte[6];
                MacGen.NextBytes(buf);
                return new MacAddress(buf);
            }
        }

        /// <summary>
        /// 按字节读写物理地址，索引只能为0-5
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0: return Byte1;
                    case 1: return Byte2;
                    case 2: return Byte3;
                    case 3: return Byte4;
                    case 4: return Byte5;
                    case 5: return Byte6;
                    default: throw new IndexOutOfRangeException();
                }
            }
            set
            {
                switch (index)
                {
                    case 0: Byte1 = value; break;
                    case 1: Byte2 = value; break;
                    case 2: Byte3 = value; break;
                    case 3: Byte4 = value; break;
                    case 4: Byte5 = value; break;
                    case 5: Byte6 = value; break;
                    default: throw new IndexOutOfRangeException();
                }
            }
        }

        public byte Byte1 { get; set; }
        public byte Byte2 { get; set; }
        public byte Byte3 { get; set; }
        public byte Byte4 { get; set; }
        public byte Byte5 { get; set; }
        public byte Byte6 { get; set; }

        public long Data
        {
            get => Byte6 | (Byte5 << 8) | (Byte4 << 16) | (Byte3 << 24) | (Byte2 << 32) | (Byte1 << 40);
            set => (Byte6, Byte5, Byte4, Byte3, Byte2, Byte1) = ((byte)value, (byte)(value >> 8), (byte)(value >> 16), (byte)(value >> 24), (byte)(value >> 32), (byte)(value >> 40));
        }

        /// <summary>
        /// 验证物理地址是否不为空且不为广播地址
        /// </summary>
        public bool IsValid => this != Empty && this != Broadcast;

        public byte[] Bytes => new byte[] { Byte1, Byte2, Byte3, Byte4, Byte5, Byte6 };

        public MacAddress(long data) : this() => Data = data;
        public MacAddress(byte[] data)
        {
            if (data.Length != 6) throw new ArgumentException("Wrong Length");
            (Byte1, Byte2, Byte3, Byte4, Byte5, Byte6) = (data[0], data[1], data[2], data[3], data[4], data[5]);
        }
        public MacAddress(int byte1, int byte2, int byte3, int byte4, int byte5, int byte6)
        => (Byte1, Byte2, Byte3, Byte4, Byte5, Byte6) = ((byte)byte1, (byte)byte2, (byte)byte3, (byte)byte4, (byte)byte5, (byte)byte6);
        public MacAddress(PhysicalAddress mac) : this(mac.GetAddressBytes()) { }

        public static MacAddress Parse(string str) => string.IsNullOrWhiteSpace(str) ? Empty : new MacAddress(PhysicalAddress.Parse(str.ToUpper()));

        public static bool TryParse(string str, out MacAddress Mac)
        {
            try
            {
                Mac = Parse(str);
                return true;
            }
            catch
            {
                Mac = default;
                return false;
            }
        }

        public override string ToString() => ToString("-");
        public string ToString(string sep) => $"{Byte1:X02}{sep}{Byte2:X02}{sep}{Byte3:X02}{sep}{Byte4:X02}{sep}{Byte5:X02}{sep}{Byte6:X02}";

        public override bool Equals(object obj) => obj is MacAddress address && Equals(address);

        public bool Equals(MacAddress other)
        {
            return Byte1 == other.Byte1 &&
                   Byte2 == other.Byte2 &&
                   Byte3 == other.Byte3 &&
                   Byte4 == other.Byte4 &&
                   Byte5 == other.Byte5 &&
                   Byte6 == other.Byte6;
        }

        public override int GetHashCode()
        {
            int hashCode = 1202161321;
            hashCode = hashCode * -1521134295 + Byte1;
            hashCode = hashCode * -1521134295 + Byte2;
            hashCode = hashCode * -1521134295 + Byte3;
            hashCode = hashCode * -1521134295 + Byte4;
            hashCode = hashCode * -1521134295 + Byte5;
            hashCode = hashCode * -1521134295 + Byte6;
            return hashCode;
        }

        public int CompareTo(MacAddress other) => (int)(ToLong() - other.ToLong());

        public long ToLong() => Data;
        public static explicit operator long(MacAddress mac) => mac.Data;
        public static explicit operator MacAddress(long mac) => new MacAddress(mac);

        public static bool operator ==(MacAddress left, MacAddress right) => left.Equals(right);
        public static bool operator !=(MacAddress left, MacAddress right) => !(left == right);

        public static implicit operator PhysicalAddress(MacAddress mac) => new PhysicalAddress(mac.Bytes);
        public static implicit operator MacAddress(PhysicalAddress mac) => new MacAddress(mac);
    }

}
