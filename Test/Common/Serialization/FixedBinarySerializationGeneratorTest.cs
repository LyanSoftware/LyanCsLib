using System.Buffers;
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
    public void ManualFixedContractCanBeInlined()
    {
        var value = new GeneratedManualOuter(
            new ManualWord(0x1234),
            [new ManualWord(1), new ManualWord(2)]);

        var bytes = value.Serialize(Endian.Big);
        var status = GeneratedManualOuter.TryDeserialize(bytes, out var decoded, Endian.Big);

        Assert.Equal(new byte[] { 0x12, 0x34, 0, 1, 0, 2 }, bytes);
        Assert.Equal(OperationStatus.Done, status);
        Assert.Equal((ushort)0x1234, decoded.Header.Value);
        Assert.Equal(new ushort[] { 1, 2 }, decoded.Values.Select(item => item.Value));
    }

    [Fact]
    public void OversizedFixedArrayLeavesDestinationUntouchedAndIsLocalized()
    {
        var value = new GeneratedPacket(1, false, [1, 2, 3, 4, 5]);
        var destination = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA, 0xAA };

        var exception = Assert.Throws<ArgumentException>(
            () => value.TrySerialize(destination, out _));

        Assert.All(destination, item => Assert.Equal((byte)0xAA, item));
        Assert.Equal(
            "Lytec.Common.Serialization.FixedBinarySerialization:ArrayLongerThanFixedLength",
            exception.GetLocalizedMessage()?.LookupKey);
    }
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
[Endian(Endian.Big)]
public partial struct GeneratedPacket
{
    public ushort Command { get; }

    [field: BinaryDataType(BinaryDataType.Boolean8)]
    public bool Enabled { get; }

    [field: BinaryFixedArray(4)]
    public byte[] Payload { get; }

    public GeneratedPacket(ushort command, bool enabled, byte[] payload)
        => (Command, Enabled, Payload) = (command, enabled, payload);
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
public partial class GeneratedBase
{
    private readonly byte _baseValue;

    public GeneratedBase(byte baseValue) => _baseValue = baseValue;
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
public partial class GeneratedDerived : GeneratedBase
{
    private readonly byte _derivedValue;

    public GeneratedDerived(byte baseValue, byte derivedValue) : base(baseValue)
        => _derivedValue = derivedValue;
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
public partial struct GeneratedInner
{
    public ushort Value { get; }

    public GeneratedInner(ushort value) => Value = value;
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
public partial struct GeneratedOuter
{
    public GeneratedInner Inner { get; }
    public byte Tail { get; }

    public GeneratedOuter(GeneratedInner inner, byte tail) => (Inner, Tail) = (inner, tail);
}

[FixedBinarySerializable(BinaryLayoutKind.Explicit, Size = 4)]
public partial struct GeneratedExplicit
{
    [BinaryFieldOffset(0)]
    public byte Prefix;

    [BinaryFieldOffset(2)]
    public ushort Number;

    public GeneratedExplicit(byte prefix, ushort number)
    {
        Prefix = prefix;
        Number = number;
    }
}

[FixedBinarySerializable(
    BinaryLayoutKind.Sequential,
    MemberInclusion = BinaryMemberInclusion.OptIn)]
public partial struct GeneratedOptIn
{
    private readonly byte _ignored;

    [BinaryMember]
    private readonly byte _included;

    public GeneratedOptIn(byte ignored, byte included)
        => (_ignored, _included) = (ignored, included);
}

[FixedBinaryContract(2, typeof(ManualWordCodecProvider))]
public readonly struct ManualWord : IBinarySerializable
{
    public ushort Value { get; }

    public int SerializedSize => 2;

    public ManualWord(ushort value) => Value = value;

    public byte[] Serialize(Endian? endian = null)
    {
        var result = new byte[SerializedSize];
        TrySerialize(result, out _, endian);
        return result;
    }

    public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? endian = null)
    {
        written = 0;
        if (destination.Length < SerializedSize)
            return OperationStatus.DestinationTooSmall;
        FixedBinaryPrimitives.WriteUInt16(
            destination,
            Value,
            FixedBinaryPrimitives.ResolveEndian(endian));
        written = SerializedSize;
        return OperationStatus.Done;
    }
}

public sealed class ManualWordCodecProvider : IFixedBinaryCodecProvider<ManualWord>
{
    public IFixedBinaryCodec<ManualWord> GetBinaryCodec(Endian? endian = null)
        => new ManualWordCodec(endian);

    private sealed class ManualWordCodec(Endian? endian) : IFixedBinaryCodec<ManualWord>
    {
        public int FixedSize => 2;
        public int MinimumFrameLength => FixedSize;
        public int MaximumFrameLength => FixedSize;
        public BinaryResynchronizationMode ResynchronizationMode => BinaryResynchronizationMode.StopOnInvalidData;

        public OperationStatus TryGetSerializedLength(ManualWord value, out int length)
        {
            length = FixedSize;
            return OperationStatus.Done;
        }

        public OperationStatus TrySerialize(ManualWord value, Span<byte> destination, out int written)
            => value.TrySerialize(destination, out written, endian);

        public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out ManualWord value)
        {
            if (source.Length < FixedSize)
            {
                value = default;
                return isFinalBlock
                    ? BinaryParseResult.InvalidData(0, source.Length)
                    : BinaryParseResult.NeedMoreData(source.Length);
            }

            value = new ManualWord(FixedBinaryPrimitives.ReadUInt16(
                source,
                FixedBinaryPrimitives.ResolveEndian(endian)));
            return BinaryParseResult.Done(FixedSize);
        }

        public IBinaryStreamDecoder<ManualWord> CreateStreamDecoder()
            => new FixedBinaryStreamDecoder<ManualWord>(this);
    }
}

[FixedBinarySerializable(BinaryLayoutKind.Sequential)]
public partial struct GeneratedManualOuter
{
    public ManualWord Header { get; }

    [field: BinaryFixedArray(2)]
    public ManualWord[] Values { get; }

    public GeneratedManualOuter(ManualWord header, ManualWord[] values)
        => (Header, Values) = (header, values);
}
