using System.Buffers;
using Lytec.Common.Localization.Extensions;
using Lytec.Common.Serialization;

namespace Test;

public class BinarySerializationInfrastructureTest
{
    [Fact]
    public void SerializeToArrayAndBufferWriter()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        byte[] value = [1, 2, 3];

        Assert.Equal(new byte[] { 4, 1, 2, 3 }, codec.Serialize(value));

        var writer = new TestBufferWriter();
        codec.Serialize(value, writer);
        Assert.Equal(new byte[] { 4, 1, 2, 3 }, writer.ToArray());
    }

    [Fact]
    public void TrySerializeDoesNotModifySmallDestination()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var destination = new byte[] { 0xAA, 0xAA };

        var status = codec.TrySerialize([1, 2], destination, out var written);

        Assert.Equal(OperationStatus.DestinationTooSmall, status);
        Assert.Equal(0, written);
        Assert.Equal(new byte[] { 0xAA, 0xAA }, destination);
    }

    [Fact]
    public void InvalidValueIsReportedWithoutWriting()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var destination = new byte[] { 0xAA, 0xAA, 0xAA, 0xAA };

        Assert.Equal(OperationStatus.InvalidData, codec.TryGetSerializedLength([], out var length));
        Assert.Equal(0, length);
        Assert.Equal(OperationStatus.InvalidData, codec.TrySerialize([], destination, out var written));
        Assert.Equal(0, written);
        Assert.All(destination, value => Assert.Equal((byte)0xAA, value));
    }

    [Fact]
    public void DatagramNeverSearchesForAnotherStartPosition()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);

        var result = codec.ParseDatagram([0, 3, 7, 8], out var value);

        Assert.Equal(OperationStatus.InvalidData, result.Status);
        Assert.Null(value);
    }

    [Fact]
    public void DatagramConvertsTruncationToInvalidData()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);

        var result = codec.ParseDatagram([4, 1], out var value);

        Assert.Equal(OperationStatus.InvalidData, result.Status);
        Assert.Null(value);
    }

    [Fact]
    public void DecoderCombinesSplitInput()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var decoder = codec.CreateStreamDecoder();

        var first = decoder.Append([4, 1], out var incomplete);
        var stateAfterFirst = decoder.State;
        var second = decoder.Append([2, 3], out var value);

        Assert.Equal(OperationStatus.NeedMoreData, first.Status);
        Assert.Equal(2, first.Consumed);
        Assert.Null(incomplete);
        Assert.Equal(BinaryDecoderState.Receiving, stateAfterFirst);
        Assert.Equal(OperationStatus.Done, second.Status);
        Assert.Equal(2, second.Consumed);
        Assert.Equal(new byte[] { 1, 2, 3 }, value);
        Assert.Equal(BinaryDecoderState.Ready, decoder.State);
    }

    [Fact]
    public void DecoderStopsAtFirstFrame()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var decoder = codec.CreateStreamDecoder();

        var result = decoder.Append([3, 1, 2, 2, 9], out var value);

        Assert.Equal(OperationStatus.Done, result.Status);
        Assert.Equal(3, result.Consumed);
        Assert.Equal(new byte[] { 1, 2 }, value);
        Assert.Equal(0, decoder.BufferedLength);
    }

    [Fact]
    public void AutomaticDecoderReportsDiscardedNoise()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var decoder = codec.CreateStreamDecoder();

        var result = decoder.Append([0, 0xFF, 3, 1, 2], out var value);

        Assert.Equal(OperationStatus.Done, result.Status);
        Assert.Equal(5, result.Consumed);
        Assert.Equal(2, result.Discarded);
        Assert.Equal(new byte[] { 1, 2 }, value);
    }

    [Fact]
    public void StopOnInvalidDataFaultsUntilReset()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.StopOnInvalidData);
        var decoder = codec.CreateStreamDecoder();

        var result = decoder.Append([0, 3, 1, 2], out var value);

        Assert.Equal(OperationStatus.InvalidData, result.Status);
        Assert.Equal(0, result.Consumed);
        Assert.Null(value);
        Assert.Equal(BinaryDecoderState.Faulted, decoder.State);
        Assert.Throws<InvalidOperationException>(() => decoder.Append(3, out _));

        decoder.Reset();
        Assert.Equal(BinaryDecoderState.Ready, decoder.State);
    }

    [Fact]
    public void CompleteDistinguishesCleanAndTruncatedStreams()
    {
        var codec = new LengthPrefixedCodec(BinaryResynchronizationMode.Automatic);
        var clean = codec.CreateStreamDecoder();
        var truncated = codec.CreateStreamDecoder();

        Assert.Equal(OperationStatus.Done, clean.Complete().Status);
        Assert.Equal(BinaryDecoderState.Completed, clean.State);

        Assert.Equal(OperationStatus.NeedMoreData, truncated.Append([4, 1], out _).Status);
        Assert.Equal(OperationStatus.InvalidData, truncated.Complete().Status);
        Assert.Equal(BinaryDecoderState.Faulted, truncated.State);
    }

    [Fact]
    public void MaximumLengthCandidateCannotRemainIncomplete()
    {
        var codec = new NeverCompleteCodec(BinaryResynchronizationMode.StopOnInvalidData);
        var decoder = codec.CreateStreamDecoder();

        var result = decoder.Append([1, 2, 3, 4], out _);

        Assert.Equal(OperationStatus.InvalidData, result.Status);
        Assert.Equal(BinaryDecoderState.Faulted, decoder.State);
    }

    [Fact]
    public void ContractExceptionsContainLocalizationInformation()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(
            () => new BinaryDecodeResult(OperationStatus.Done, -1, 0));

        var localizedMessage = exception.GetLocalizedMessage();
        Assert.NotNull(localizedMessage);
        Assert.Equal(
            "Lytec.Common.Serialization.BinaryDecodeResult:NegativeConsumed",
            localizedMessage.Key);
    }

    private sealed class LengthPrefixedCodec : IBinaryCodec<byte[]>
    {
        public int MinimumFrameLength => 2;

        public int MaximumFrameLength => 8;

        public BinaryResynchronizationMode ResynchronizationMode { get; }

        public LengthPrefixedCodec(BinaryResynchronizationMode resynchronizationMode)
            => ResynchronizationMode = resynchronizationMode;

        public OperationStatus TryGetSerializedLength(byte[] value, out int length)
        {
            if (value == null || value.Length == 0 || value.Length >= MaximumFrameLength)
            {
                length = 0;
                return OperationStatus.InvalidData;
            }

            length = value.Length + 1;
            return OperationStatus.Done;
        }

        public OperationStatus TrySerialize(byte[] value, Span<byte> destination, out int written)
        {
            var status = TryGetSerializedLength(value, out var length);
            if (status != OperationStatus.Done)
            {
                written = 0;
                return status;
            }
            if (destination.Length < length)
            {
                written = 0;
                return OperationStatus.DestinationTooSmall;
            }

            destination[0] = (byte)length;
            value.CopyTo(destination.Slice(1));
            written = length;
            return OperationStatus.Done;
        }

        public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out byte[] value)
        {
            value = null!;
            if (source.IsEmpty)
                return isFinalBlock
                    ? BinaryParseResult.InvalidData(0, 0)
                    : BinaryParseResult.NeedMoreData(0);

            var length = source[0];
            if (length < MinimumFrameLength || length > MaximumFrameLength)
                return BinaryParseResult.InvalidData(1, 1);
            if (source.Length < length)
                return isFinalBlock
                    ? BinaryParseResult.InvalidData(0, source.Length)
                    : BinaryParseResult.NeedMoreData(source.Length);

            value = source.Slice(1, length - 1).ToArray();
            return BinaryParseResult.Done(length);
        }

        public IBinaryStreamDecoder<byte[]> CreateStreamDecoder()
            => new BufferedBinaryStreamDecoder<byte[]>(this, ResynchronizationMode);
    }

    private sealed class NeverCompleteCodec : IBinaryCodec<byte[]>
    {
        public int MinimumFrameLength => 1;

        public int MaximumFrameLength => 4;

        public BinaryResynchronizationMode ResynchronizationMode { get; }

        public NeverCompleteCodec(BinaryResynchronizationMode resynchronizationMode)
            => ResynchronizationMode = resynchronizationMode;

        public OperationStatus TryGetSerializedLength(byte[] value, out int length)
        {
            length = 0;
            return OperationStatus.InvalidData;
        }

        public OperationStatus TrySerialize(byte[] value, Span<byte> destination, out int written)
        {
            written = 0;
            return OperationStatus.InvalidData;
        }

        public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out byte[] value)
        {
            value = null!;
            return BinaryParseResult.NeedMoreData(source.Length);
        }

        public IBinaryStreamDecoder<byte[]> CreateStreamDecoder()
            => new BufferedBinaryStreamDecoder<byte[]>(this, ResynchronizationMode);
    }

    private sealed class TestBufferWriter : IBufferWriter<byte>
    {
        private byte[] _buffer = new byte[16];
        private int _written;

        public void Advance(int count) => _written += count;

        public Memory<byte> GetMemory(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsMemory(_written);
        }

        public Span<byte> GetSpan(int sizeHint = 0)
        {
            EnsureCapacity(sizeHint);
            return _buffer.AsSpan(_written);
        }

        public byte[] ToArray() => _buffer.AsSpan(0, _written).ToArray();

        private void EnsureCapacity(int sizeHint)
        {
            var required = _written + Math.Max(1, sizeHint);
            if (required > _buffer.Length)
                Array.Resize(ref _buffer, required);
        }
    }
}
