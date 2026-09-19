using System.Buffers;
using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Lytec.Common.Localization.Extensions;
using Lytec.Common.Serialization;

namespace Test;

public class FixedBinarySerializationGeneratorTest
{
    [Fact]
    public void GeneratedCodecRoundTripsSequentialObject()
    {
        var value = new GeneratedPacket(0x1234, true, [1, 2, 3]);

        var bytes = value.Serialize();
        var status = GeneratedPacket.TryDeserialize(bytes, out var decoded);

        Assert.Equal(new byte[] { 0x12, 0x34, 1, 1, 2, 3, 0 }, bytes);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal((ushort)0x1234, decoded.Command);
        Assert.True(decoded.Enabled);
        Assert.Equal(new byte[] { 1, 2, 3, 0 }, decoded.Payload);
    }

    [Fact]
    public void DirectDeserializationRequiresExactLength()
    {
        Assert.Equal(OperationStatus.InvalidData, GeneratedPacket.TryDeserialize(new byte[6], out _));
        Assert.Equal(OperationStatus.InvalidData, GeneratedPacket.TryDeserialize(new byte[8], out _));

        var parsed = GeneratedPacket.BinaryCodec.Parse(new byte[8], true, out _);
        Assert.Equal(OperationStatus.Done, parsed.Status);
        Assert.Equal(GeneratedPacket.BinaryCodec.FixedSize, parsed.Consumed);
    }

    [Fact]
    public void FixedDecoderSupportsPreviewAndManualRecovery()
    {
        var decoder = GeneratedPacket.BinaryCodec.CreateStreamDecoder();
        Assert.Equal(OperationStatus.NeedMoreData, decoder.Append([0x12, 0x34], out _).Status);
        var fixedDecoder = Assert.IsAssignableFrom<IFixedBinaryStreamDecoder<GeneratedPacket>>(decoder);

        var preview = fixedDecoder.TryGetTemporary(
            static (_, missing) => { missing.Clear(); return true; },
            out var temporary);

        Assert.Equal(OperationStatus.Done, preview);
        Assert.Equal((ushort)0x1234, temporary.Command);
        Assert.Equal(2, fixedDecoder.BufferedLength);
        Assert.True(fixedDecoder.TryDiscardOldest(1));
        Assert.Equal(1, fixedDecoder.BufferedLength);
    }

    [Fact]
    public void DerivedRuntimeSizeMismatchIsRejected()
    {
        GeneratedBase value = new GeneratedDerived(1, 2);

        Assert.Throws<InvalidOperationException>(
            () => GeneratedBase.BinaryCodec.TryGetSerializedLength(value, out _));
    }

    [Fact]
    public void NestedAndExplicitLayoutsUseCalculatedOffsets()
    {
        var nested = new GeneratedOuter(new GeneratedInner(0x1234), 0x56);
        var explicitValue = new GeneratedExplicit(0x11, 0x2233);

        Assert.Equal(new byte[] { 0x12, 0x34, 0x56 }, nested.Serialize(Endian.Big));
        Assert.Equal(new byte[] { 0x11, 0, 0x22, 0x33 }, explicitValue.Serialize(Endian.Big));
        Assert.Equal(OperationStatus.Done, GeneratedExplicit.TryDeserialize(
            [0x11, 0, 0x22, 0x33], out var decoded, Endian.Big));
        Assert.Equal((ushort)0x2233, decoded.Number);
    }

    [Fact]
    public void OptInExcludesUnmarkedStorage()
    {
        var value = new GeneratedOptIn(1, 2);

        Assert.Equal(1, value.SerializedSize);
        Assert.Equal(new byte[] { 2 }, value.Serialize());
    }

    [Fact]
    public void OversizedByValArrayLeavesDestinationUntouchedAndIsLocalized()
    {
        var value = new GeneratedPacket(1, false, [1, 2, 3, 4, 5]);
        var destination = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        var exception = Assert.Throws<ArgumentException>(
            () => value.TrySerialize(destination, out _));

        Assert.All(destination, item => Assert.Equal((byte)0xAA, item));
        Assert.Equal(
            "Lytec.Common.Serialization.FixedBinarySerialization:ArrayLongerThanSizeConst",
            exception.GetLocalizedMessage()?.Key);
    }
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Big)]
public partial struct GeneratedPacket : IBinarySerializable
{
    public ushort Command { get; }

    [field: MarshalAs(UnmanagedType.U1)]
    public bool Enabled { get; }

    [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4, ArraySubType = UnmanagedType.U1)]
    public byte[] Payload { get; }

    public GeneratedPacket(ushort command, bool enabled, byte[] payload)
        => (Command, Enabled, Payload) = (command, enabled, payload);
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial class GeneratedBase : IBinarySerializable
{
    private readonly byte _baseValue;

    public GeneratedBase(byte baseValue) => _baseValue = baseValue;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial class GeneratedDerived : GeneratedBase, IBinarySerializable
{
    private readonly byte _derivedValue;

    public GeneratedDerived(byte baseValue, byte derivedValue) : base(baseValue)
        => _derivedValue = derivedValue;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct GeneratedInner : IBinarySerializable
{
    public ushort Value { get; }

    public GeneratedInner(ushort value) => Value = value;
}

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct GeneratedOuter : IBinarySerializable
{
    public GeneratedInner Inner { get; }
    public byte Tail { get; }

    public GeneratedOuter(GeneratedInner inner, byte tail) => (Inner, Tail) = (inner, tail);
}

[StructLayout(LayoutKind.Explicit, Pack = 1, Size = 4)]
public partial struct GeneratedExplicit : IBinarySerializable
{
    [FieldOffset(0)]
    public byte Prefix;

    [FieldOffset(2)]
    public ushort Number;

    public GeneratedExplicit(byte prefix, ushort number)
    {
        Prefix = prefix;
        Number = number;
    }
}

[BinaryObject(BinaryMemberInclusion.OptIn)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
public partial struct GeneratedOptIn : IBinarySerializable
{
    private readonly byte _ignored;

    [BinaryMember]
    private readonly byte _included;

    public GeneratedOptIn(byte ignored, byte included)
        => (_ignored, _included) = (ignored, included);
}
