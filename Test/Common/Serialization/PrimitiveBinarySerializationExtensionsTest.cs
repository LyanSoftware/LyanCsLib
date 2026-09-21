using System.Buffers;
using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Lytec.Common.Serialization;

namespace Test;

public class PrimitiveBinarySerializationExtensionsTest
{
    [Fact]
    public void SerializeToBytesUsesRequestedEndian()
    {
        Assert.Equal(new byte[] { 0xFE }, ((sbyte)-2).SerializeToBytes(Endian.Big));
        Assert.Equal(new byte[] { 0xAB }, ((byte)0xAB).SerializeToBytes(Endian.Little));
        Assert.Equal(new byte[] { 0x12, 0x34 }, ((short)0x1234).SerializeToBytes(Endian.Big));
        Assert.Equal(new byte[] { 0x34, 0x12 }, ((ushort)0x1234).SerializeToBytes(Endian.Little));
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, 0x12345678.SerializeToBytes(Endian.Big));
        Assert.Equal(new byte[] { 0x78, 0x56, 0x34, 0x12 }, 0x12345678U.SerializeToBytes(Endian.Little));
        Assert.Equal(
            new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 },
            0x0102030405060708L.SerializeToBytes(Endian.Big));
        Assert.Equal(
            new byte[] { 8, 7, 6, 5, 4, 3, 2, 1 },
            0x0102030405060708UL.SerializeToBytes(Endian.Little));
        Assert.Equal(new byte[] { 0x3F, 0x80, 0, 0 }, 1F.SerializeToBytes(Endian.Big));
        Assert.Equal(
            new byte[] { 0x3F, 0xF0, 0, 0, 0, 0, 0, 0 },
            1D.SerializeToBytes(Endian.Big));
    }

    [Fact]
    public void EveryNumericTypeCanRoundTrip()
    {
        Assert.Equal(OperationStatus.Done, ((sbyte)-12).SerializeToBytes().TryDeserializeToSByte(out var sbyteValue));
        Assert.Equal((sbyte)-12, sbyteValue);
        Assert.Equal(OperationStatus.Done, ((byte)234).SerializeToBytes().TryDeserializeToByte(out var byteValue));
        Assert.Equal((byte)234, byteValue);
        Assert.Equal(OperationStatus.Done, ((short)-12345).SerializeToBytes(Endian.Big).TryDeserializeToInt16(out var int16Value, Endian.Big));
        Assert.Equal((short)-12345, int16Value);
        Assert.Equal(OperationStatus.Done, ((ushort)54321).SerializeToBytes(Endian.Big).TryDeserializeToUInt16(out var uint16Value, Endian.Big));
        Assert.Equal((ushort)54321, uint16Value);
        Assert.Equal(OperationStatus.Done, (-123456789).SerializeToBytes(Endian.Big).TryDeserializeToInt32(out var int32Value, Endian.Big));
        Assert.Equal(-123456789, int32Value);
        Assert.Equal(OperationStatus.Done, 3456789012U.SerializeToBytes(Endian.Big).TryDeserializeToUInt32(out var uint32Value, Endian.Big));
        Assert.Equal(3456789012U, uint32Value);
        Assert.Equal(OperationStatus.Done, (-1234567890123456789L).SerializeToBytes(Endian.Big).TryDeserializeToInt64(out var int64Value, Endian.Big));
        Assert.Equal(-1234567890123456789L, int64Value);
        Assert.Equal(OperationStatus.Done, 12345678901234567890UL.SerializeToBytes(Endian.Big).TryDeserializeToUInt64(out var uint64Value, Endian.Big));
        Assert.Equal(12345678901234567890UL, uint64Value);
        Assert.Equal(OperationStatus.Done, 123.5F.SerializeToBytes(Endian.Big).TryDeserializeToSingle(out var singleValue, Endian.Big));
        Assert.Equal(123.5F, singleValue);
        Assert.Equal(OperationStatus.Done, (-123.5D).SerializeToBytes(Endian.Big).TryDeserializeToDouble(out var doubleValue, Endian.Big));
        Assert.Equal(-123.5D, doubleValue);
    }

    [Fact]
    public void DeserializationRequiresExactLength()
    {
        Assert.Equal(OperationStatus.InvalidData, Array.Empty<byte>().TryDeserializeToByte(out var byteValue));
        Assert.Equal((byte)0, byteValue);
        Assert.Equal(OperationStatus.InvalidData, new byte[3].TryDeserializeToInt32(out var shortValue));
        Assert.Equal(0, shortValue);
        Assert.Equal(OperationStatus.InvalidData, new byte[5].TryDeserializeToInt32(out var longValue));
        Assert.Equal(0, longValue);

        ReadOnlySpan<byte> source = new byte[] { 0x12, 0x34 };
        Assert.Equal(OperationStatus.Done, source.TryDeserializeToUInt16(out var value, Endian.Big));
        Assert.Equal((ushort)0x1234, value);
    }

    [Fact]
    public void BooleanUsesExplicitRepresentation()
    {
        Assert.Equal(new byte[] { 1 }, true.SerializeToBytes(UnmanagedType.I1));
        Assert.Equal(new byte[] { 1 }, true.SerializeToBytes(UnmanagedType.U1));
        Assert.Equal(new byte[] { 0, 1 }, true.SerializeToBytes(UnmanagedType.I2, Endian.Big));
        Assert.Equal(new byte[] { 1, 0 }, true.SerializeToBytes(UnmanagedType.U2, Endian.Little));
        Assert.Equal(new byte[] { 0, 0, 0, 1 }, true.SerializeToBytes(UnmanagedType.Bool, Endian.Big));
        Assert.Equal(new byte[] { 0, 0, 0, 1 }, true.SerializeToBytes(UnmanagedType.I4, Endian.Big));
        Assert.Equal(new byte[] { 1, 0, 0, 0 }, true.SerializeToBytes(UnmanagedType.U4, Endian.Little));
        Assert.Equal(new byte[] { 0, 0, 0, 0 }, false.SerializeToBytes(UnmanagedType.Bool, Endian.Big));
    }

    [Fact]
    public void BooleanDeserializationAcceptsEveryNonzeroValueIncludingPascalTrue()
    {
        Assert.Equal(
            OperationStatus.Done,
            new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }.TryDeserializeToBoolean(
                UnmanagedType.Bool,
                out var pascalValue,
                Endian.Big));
        Assert.True(pascalValue);

        Assert.Equal(
            OperationStatus.Done,
            new byte[] { 0x80 }.TryDeserializeToBoolean(UnmanagedType.I1, out var nonCanonicalValue));
        Assert.True(nonCanonicalValue);
    }

    [Fact]
    public void InvalidArgumentsAreRejected()
    {
        Assert.Throws<ArgumentOutOfRangeException>(
            () => true.SerializeToBytes(UnmanagedType.LPStr));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new byte[4].TryDeserializeToBoolean(UnmanagedType.LPStr, out _));
        Assert.Throws<ArgumentOutOfRangeException>(
            () => 1.SerializeToBytes((Endian)123));

        byte[]? source = null;
        Assert.Throws<ArgumentNullException>(() => source!.TryDeserializeToInt32(out _));
    }
}
