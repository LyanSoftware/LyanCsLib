using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using Lytec.Common;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct Fat16Date
    {
        public const int SizeConst = 2;
        static Fat16Date() => Debug.Assert(Marshal.SizeOf<Fat16Date>() == SizeConst);

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

        public Fat16Date(DateTime date) : this() => (Year, Month, Day) = (date.Year, date.Month, date.Day);

        public override string ToString() => ((DateTime)this).ToString();

        public static implicit operator DateTime(Fat16Date date) => new DateTime(date.Year, date.Month, date.Day);
        public static implicit operator Fat16Date(DateTime date) => new Fat16Date(date);
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct Fat16Time
    {
        public const int SizeConst = 2;
        static Fat16Time() => Debug.Assert(Marshal.SizeOf<Fat16Time>() == SizeConst);

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

        public Fat16Time(DateTime time) : this() => (Hour, Minute, Second) = (time.Hour, time.Minute, time.Second);

        public override string ToString() => ((DateTime)this).ToString();

        public static implicit operator DateTime(Fat16Time time) => new DateTime(0, 0, 0, time.Hour, time.Minute, time.Second);
        public static implicit operator Fat16Time(DateTime time) => new Fat16Time(time);
    }

    [Serializable]
    [Endian(DefaultEndian)]
    public enum Fat16ItemType : byte
    {
        File = 0x00,
        Dir = 0x10,
    }

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public struct Fat16ItemInfo : IPackage
    {
        public const int SizeConst = 32;
        static Fat16ItemInfo() => Debug.Assert(Marshal.SizeOf<Fat16ItemInfo>() == SizeConst);

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
        public int NameMaxLength => Type == Fat16ItemType.Dir ? 3 : 8;
        public int ExtMaxLength => Type == Fat16ItemType.Dir ? 0 : 3;
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
        public Fat16ItemType Type { get; set; }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        private readonly byte[] _Unused;

        public Fat16Time CreateTime { get; set; }
        public Fat16Date CreateDate { get; set; }

        private readonly ushort _Unused2;

        public uint FileSize { get; set; }

        public byte[] Serialize() => this.ToBytes();
        public static Fat16ItemInfo Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<Fat16ItemInfo>(offset, DefaultEndian);
        public static Fat16ItemInfo[] DeserializeAll(byte[] bytes, int offset = 0) => bytes.ToStruct<Fat16ItemInfo[]>(offset, DefaultEndian);
    }

    [Serializable]
    [Endian(DefaultEndian)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct FileInfo : IPackage
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Attribute : byte
        {
            NormalFile = 0,
            Directory = 0x10,
        }

        public string FileName
        {
            get
            {
                var name = $"{Name.Trim()}.{Extension.Trim()}";
                switch (name)
                {
                    case "..": return ".";
                    case "...": return "..";
                    default: return name;
                }
            }
        }

        public string Name => _Name != null ? DefaultEncode.GetString(_Name) : "";
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
        private readonly byte[] _Name;

        public string Extension => _Ext != null ? DefaultEncode.GetString(_Ext) : "";
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        private readonly byte[] _Ext;

        public Attribute Attributes { get; }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 10)]
        private readonly byte[] _unused;

        public Fat16Time Time { get; }
        public Fat16Date Date { get; }

        private readonly ushort _unused1;

        public uint Length { get; }

        public override string ToString()
        => $"FileName: \"{FileName}\", Length: {Length}, Attributes: {Attributes.GetDescription()}, DateTime: {new DateTime(Date.Year, Date.Month, Date.Day, Time.Hour, Time.Minute, Time.Second)}";

        public byte[] Serialize() => this.ToBytes();
        public static FileInfo Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<FileInfo>(offset);
    }

}
