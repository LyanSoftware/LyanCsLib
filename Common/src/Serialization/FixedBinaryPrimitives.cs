using System;
using System.Buffers.Binary;
using System.Runtime.InteropServices;
using Lytec.Common.Data;

namespace Lytec.Common.Serialization;

/// <summary>供生成代码使用的固定宽度基元读写方法。</summary>
public static class FixedBinaryPrimitives
{
    public static Endian ResolveEndian(Endian? endian)
    {
        var resolved = endian ?? (BitConverter.IsLittleEndian ? Endian.Little : Endian.Big);
        if (resolved != Endian.Little && resolved != Endian.Big)
            throw new ArgumentOutOfRangeException(nameof(endian));
        return resolved;
    }

    public static void WriteInt16(Span<byte> destination, short value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteInt16LittleEndian(destination, value); else BinaryPrimitives.WriteInt16BigEndian(destination, value); }
    public static short ReadInt16(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadInt16LittleEndian(source) : BinaryPrimitives.ReadInt16BigEndian(source);
    public static void WriteUInt16(Span<byte> destination, ushort value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteUInt16LittleEndian(destination, value); else BinaryPrimitives.WriteUInt16BigEndian(destination, value); }
    public static ushort ReadUInt16(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadUInt16LittleEndian(source) : BinaryPrimitives.ReadUInt16BigEndian(source);
    public static void WriteInt32(Span<byte> destination, int value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteInt32LittleEndian(destination, value); else BinaryPrimitives.WriteInt32BigEndian(destination, value); }
    public static int ReadInt32(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadInt32LittleEndian(source) : BinaryPrimitives.ReadInt32BigEndian(source);
    public static void WriteUInt32(Span<byte> destination, uint value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteUInt32LittleEndian(destination, value); else BinaryPrimitives.WriteUInt32BigEndian(destination, value); }
    public static uint ReadUInt32(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadUInt32LittleEndian(source) : BinaryPrimitives.ReadUInt32BigEndian(source);
    public static void WriteInt64(Span<byte> destination, long value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteInt64LittleEndian(destination, value); else BinaryPrimitives.WriteInt64BigEndian(destination, value); }
    public static long ReadInt64(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadInt64LittleEndian(source) : BinaryPrimitives.ReadInt64BigEndian(source);
    public static void WriteUInt64(Span<byte> destination, ulong value, Endian endian)
    { if (endian == Endian.Little) BinaryPrimitives.WriteUInt64LittleEndian(destination, value); else BinaryPrimitives.WriteUInt64BigEndian(destination, value); }
    public static ulong ReadUInt64(ReadOnlySpan<byte> source, Endian endian)
        => endian == Endian.Little ? BinaryPrimitives.ReadUInt64LittleEndian(source) : BinaryPrimitives.ReadUInt64BigEndian(source);

    public static void WriteSingle(Span<byte> destination, float value, Endian endian)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
#if NET8_0_OR_GREATER
        MemoryMarshal.Write(bytes, in value);
#else
        MemoryMarshal.Write(bytes, ref value);
#endif
        if ((endian == Endian.Little) != BitConverter.IsLittleEndian)
            bytes.Reverse();
        bytes.CopyTo(destination);
    }
    public static float ReadSingle(ReadOnlySpan<byte> source, Endian endian)
    {
        Span<byte> bytes = stackalloc byte[sizeof(float)];
        source.Slice(0, bytes.Length).CopyTo(bytes);
        if ((endian == Endian.Little) != BitConverter.IsLittleEndian)
            bytes.Reverse();
        return MemoryMarshal.Read<float>(bytes);
    }
    public static void WriteDouble(Span<byte> destination, double value, Endian endian)
        => WriteInt64(destination, BitConverter.DoubleToInt64Bits(value), endian);
    public static double ReadDouble(ReadOnlySpan<byte> source, Endian endian)
        => BitConverter.Int64BitsToDouble(ReadInt64(source, endian));

}
