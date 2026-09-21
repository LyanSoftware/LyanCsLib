using System;
using System.Buffers;
using System.Runtime.InteropServices;
using Lytec.Common.Data;

namespace Lytec.Common.Serialization;

/// <summary>提供固定宽度基元类型的便捷二进制序列化与反序列化方法。</summary>
public static class PrimitiveBinarySerializationExtensions
{
    /// <summary>把值序列化为单字节数组。</summary>
    public static byte[] SerializeToBytes(this sbyte value, Endian? endian = null)
    {
        FixedBinaryPrimitives.ResolveEndian(endian);
        return [unchecked((byte)value)];
    }

    /// <summary>把值序列化为单字节数组。</summary>
    public static byte[] SerializeToBytes(this byte value, Endian? endian = null)
    {
        FixedBinaryPrimitives.ResolveEndian(endian);
        return [value];
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this short value, Endian? endian = null)
    {
        var result = new byte[sizeof(short)];
        FixedBinaryPrimitives.WriteInt16(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this ushort value, Endian? endian = null)
    {
        var result = new byte[sizeof(ushort)];
        FixedBinaryPrimitives.WriteUInt16(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this int value, Endian? endian = null)
    {
        var result = new byte[sizeof(int)];
        FixedBinaryPrimitives.WriteInt32(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this uint value, Endian? endian = null)
    {
        var result = new byte[sizeof(uint)];
        FixedBinaryPrimitives.WriteUInt32(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this long value, Endian? endian = null)
    {
        var result = new byte[sizeof(long)];
        FixedBinaryPrimitives.WriteInt64(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this ulong value, Endian? endian = null)
    {
        var result = new byte[sizeof(ulong)];
        FixedBinaryPrimitives.WriteUInt64(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this float value, Endian? endian = null)
    {
        var result = new byte[sizeof(float)];
        FixedBinaryPrimitives.WriteSingle(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>把值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this double value, Endian? endian = null)
    {
        var result = new byte[sizeof(double)];
        FixedBinaryPrimitives.WriteDouble(result, value, FixedBinaryPrimitives.ResolveEndian(endian));
        return result;
    }

    /// <summary>使用显式非托管表示把布尔值序列化为精确长度的字节数组。</summary>
    public static byte[] SerializeToBytes(this bool value, UnmanagedType representation, Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        var result = new byte[GetBooleanSize(representation)];
        switch (representation)
        {
            case UnmanagedType.I1:
            case UnmanagedType.U1:
                result[0] = value ? (byte)1 : (byte)0;
                break;
            case UnmanagedType.I2:
                FixedBinaryPrimitives.WriteInt16(
                    result,
                    value ? (short)1 : (short)0,
                    resolvedEndian);
                break;
            case UnmanagedType.U2:
                FixedBinaryPrimitives.WriteUInt16(result, value ? (ushort)1 : (ushort)0, resolvedEndian);
                break;
            case UnmanagedType.Bool:
            case UnmanagedType.I4:
                FixedBinaryPrimitives.WriteInt32(result, value ? 1 : 0, resolvedEndian);
                break;
            case UnmanagedType.U4:
                FixedBinaryPrimitives.WriteUInt32(result, value ? 1U : 0U, resolvedEndian);
                break;
        }
        return result;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="sbyte"/>。</summary>
    public static OperationStatus TryDeserializeToSByte(
        this ReadOnlySpan<byte> source,
        out sbyte value,
        Endian? endian = null)
    {
        FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(sbyte))
            return Invalid(out value);
        value = unchecked((sbyte)source[0]);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="byte"/>。</summary>
    public static OperationStatus TryDeserializeToByte(
        this ReadOnlySpan<byte> source,
        out byte value,
        Endian? endian = null)
    {
        FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(byte))
            return Invalid(out value);
        value = source[0];
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="short"/>。</summary>
    public static OperationStatus TryDeserializeToInt16(
        this ReadOnlySpan<byte> source,
        out short value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(short))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadInt16(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="ushort"/>。</summary>
    public static OperationStatus TryDeserializeToUInt16(
        this ReadOnlySpan<byte> source,
        out ushort value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(ushort))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadUInt16(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="int"/>。</summary>
    public static OperationStatus TryDeserializeToInt32(
        this ReadOnlySpan<byte> source,
        out int value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(int))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadInt32(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="uint"/>。</summary>
    public static OperationStatus TryDeserializeToUInt32(
        this ReadOnlySpan<byte> source,
        out uint value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(uint))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadUInt32(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="long"/>。</summary>
    public static OperationStatus TryDeserializeToInt64(
        this ReadOnlySpan<byte> source,
        out long value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(long))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadInt64(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="ulong"/>。</summary>
    public static OperationStatus TryDeserializeToUInt64(
        this ReadOnlySpan<byte> source,
        out ulong value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(ulong))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadUInt64(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="float"/>。</summary>
    public static OperationStatus TryDeserializeToSingle(
        this ReadOnlySpan<byte> source,
        out float value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(float))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadSingle(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的输入反序列化一个 <see cref="double"/>。</summary>
    public static OperationStatus TryDeserializeToDouble(
        this ReadOnlySpan<byte> source,
        out double value,
        Endian? endian = null)
    {
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != sizeof(double))
            return Invalid(out value);
        value = FixedBinaryPrimitives.ReadDouble(source, resolvedEndian);
        return OperationStatus.Done;
    }

    /// <summary>使用显式非托管表示从精确长度的输入反序列化一个布尔值。</summary>
    public static OperationStatus TryDeserializeToBoolean(
        this ReadOnlySpan<byte> source,
        UnmanagedType representation,
        out bool value,
        Endian? endian = null)
    {
        var size = GetBooleanSize(representation);
        var resolvedEndian = FixedBinaryPrimitives.ResolveEndian(endian);
        if (source.Length != size)
            return Invalid(out value);

        value = representation switch
        {
            UnmanagedType.I1 or UnmanagedType.U1 => source[0] != 0,
            UnmanagedType.I2 => FixedBinaryPrimitives.ReadInt16(source, resolvedEndian) != 0,
            UnmanagedType.U2 => FixedBinaryPrimitives.ReadUInt16(source, resolvedEndian) != 0,
            UnmanagedType.Bool or UnmanagedType.I4 =>
                FixedBinaryPrimitives.ReadInt32(source, resolvedEndian) != 0,
            UnmanagedType.U4 => FixedBinaryPrimitives.ReadUInt32(source, resolvedEndian) != 0,
            _ => false,
        };
        return OperationStatus.Done;
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="sbyte"/>。</summary>
    public static OperationStatus TryDeserializeToSByte(
        this byte[] source,
        out sbyte value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToSByte(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="byte"/>。</summary>
    public static OperationStatus TryDeserializeToByte(
        this byte[] source,
        out byte value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToByte(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="short"/>。</summary>
    public static OperationStatus TryDeserializeToInt16(
        this byte[] source,
        out short value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToInt16(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="ushort"/>。</summary>
    public static OperationStatus TryDeserializeToUInt16(
        this byte[] source,
        out ushort value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToUInt16(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="int"/>。</summary>
    public static OperationStatus TryDeserializeToInt32(
        this byte[] source,
        out int value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToInt32(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="uint"/>。</summary>
    public static OperationStatus TryDeserializeToUInt32(
        this byte[] source,
        out uint value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToUInt32(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="long"/>。</summary>
    public static OperationStatus TryDeserializeToInt64(
        this byte[] source,
        out long value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToInt64(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="ulong"/>。</summary>
    public static OperationStatus TryDeserializeToUInt64(
        this byte[] source,
        out ulong value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToUInt64(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="float"/>。</summary>
    public static OperationStatus TryDeserializeToSingle(
        this byte[] source,
        out float value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToSingle(out value, endian);
    }

    /// <summary>从精确长度的数组反序列化一个 <see cref="double"/>。</summary>
    public static OperationStatus TryDeserializeToDouble(
        this byte[] source,
        out double value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToDouble(out value, endian);
    }

    /// <summary>使用显式非托管表示从精确长度的数组反序列化一个布尔值。</summary>
    public static OperationStatus TryDeserializeToBoolean(
        this byte[] source,
        UnmanagedType representation,
        out bool value,
        Endian? endian = null)
    {
        ArgumentNullException.ThrowIfNull(source);
        return ((ReadOnlySpan<byte>)source).TryDeserializeToBoolean(representation, out value, endian);
    }

    private static OperationStatus Invalid<T>(out T value)
    {
        value = default!;
        return OperationStatus.InvalidData;
    }

    private static int GetBooleanSize(UnmanagedType representation)
        => representation switch
        {
            UnmanagedType.I1 or UnmanagedType.U1 => 1,
            UnmanagedType.I2 or UnmanagedType.U2 => 2,
            UnmanagedType.Bool or UnmanagedType.I4 or UnmanagedType.U4 => 4,
            _ => throw new ArgumentOutOfRangeException(nameof(representation)),
        };
}
