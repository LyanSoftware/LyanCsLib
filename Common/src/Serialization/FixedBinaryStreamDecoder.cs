using System;
using System.Buffers;

namespace Lytec.Common.Serialization;

/// <summary>固定长度编解码器的默认增量解码器。</summary>
public sealed class FixedBinaryStreamDecoder<T> : IFixedBinaryStreamDecoder<T>
{
    private readonly IFixedBinaryCodec<T> _codec;
    private readonly byte[] _buffer;
    private int _bufferedLength;

    public BinaryDecoderState State { get; private set; }

    public int BufferedLength => _bufferedLength;

    public int RemainingLength => _codec.FixedSize - _bufferedLength;

    public FixedBinaryStreamDecoder(IFixedBinaryCodec<T> codec)
    {
        ArgumentNullException.ThrowIfNull(codec);
        if (codec.FixedSize <= 0)
            throw new ArgumentOutOfRangeException(nameof(codec));
        if (codec.MinimumFrameLength != codec.FixedSize || codec.MaximumFrameLength != codec.FixedSize)
            throw new ArgumentException("固定长度编解码器的长度边界必须等于 FixedSize。", nameof(codec));

        _codec = codec;
        _buffer = new byte[codec.FixedSize];
        State = BinaryDecoderState.Ready;
    }

    public BinaryDecodeResult Append(byte value, out T result)
    {
        Span<byte> source = stackalloc byte[1];
        source[0] = value;
        return Append(source, out result);
    }

    public BinaryDecodeResult Append(ReadOnlySpan<byte> source, out T result)
    {
        EnsureCanAppend();
        result = default!;
        if (source.IsEmpty)
            return new BinaryDecodeResult(OperationStatus.NeedMoreData, 0, 0);

        var copied = Math.Min(source.Length, RemainingLength);
        source.Slice(0, copied).CopyTo(_buffer.AsSpan(_bufferedLength));
        _bufferedLength += copied;
        if (_bufferedLength < _codec.FixedSize)
        {
            State = BinaryDecoderState.Receiving;
            return new BinaryDecodeResult(OperationStatus.NeedMoreData, copied, 0);
        }

        var parsed = _codec.Parse(_buffer, true, out result);
        BinarySerializerExtensions.ValidateParseResult(parsed, _buffer.Length, true);
        if (parsed.Status == OperationStatus.Done && parsed.Consumed == _codec.FixedSize)
        {
            _bufferedLength = 0;
            State = BinaryDecoderState.Ready;
            return new BinaryDecodeResult(OperationStatus.Done, copied, 0);
        }

        result = default!;
        State = BinaryDecoderState.Faulted;
        return new BinaryDecodeResult(OperationStatus.InvalidData, copied, 0);
    }

    public OperationStatus TryGetTemporary(TryFillMissingBytes fillMissing, out T value)
    {
        ArgumentNullException.ThrowIfNull(fillMissing);
        value = default!;

        var temporary = new byte[_codec.FixedSize];
        _buffer.AsSpan(0, _bufferedLength).CopyTo(temporary);
        if (_bufferedLength < temporary.Length
            && !fillMissing(_bufferedLength, temporary.AsSpan(_bufferedLength)))
            return OperationStatus.NeedMoreData;

        var parsed = _codec.Parse(temporary, true, out value);
        BinarySerializerExtensions.ValidateParseResult(parsed, temporary.Length, true);
        if (parsed.Status == OperationStatus.Done && parsed.Consumed == temporary.Length)
            return OperationStatus.Done;

        value = default!;
        return OperationStatus.InvalidData;
    }

    public bool TryDiscardOldest(int count)
    {
        if (State == BinaryDecoderState.Completed || count < 0 || count > _bufferedLength)
            return false;
        if (count == 0)
            return true;
        var remaining = _bufferedLength - count;
        if (remaining > 0)
            Buffer.BlockCopy(_buffer, count, _buffer, 0, remaining);
        _bufferedLength = remaining;

        State = _bufferedLength == 0 ? BinaryDecoderState.Ready : BinaryDecoderState.Receiving;
        return true;
    }

    public BinaryDecodeResult Complete()
    {
        if (State is BinaryDecoderState.Completed or BinaryDecoderState.Faulted)
            throw BinarySerializationExceptionFactory.InvalidOperation(
                "CompleteRequiresReset",
                "再次完成解码器之前必须先调用 Reset。");

        if (_bufferedLength == 0)
        {
            State = BinaryDecoderState.Completed;
            return new BinaryDecodeResult(OperationStatus.Done, 0, 0);
        }

        State = BinaryDecoderState.Faulted;
        return new BinaryDecodeResult(OperationStatus.InvalidData, 0, 0);
    }

    public void Reset()
    {
        _bufferedLength = 0;
        State = BinaryDecoderState.Ready;
    }

    private void EnsureCanAppend()
    {
        if (State is BinaryDecoderState.Completed or BinaryDecoderState.Faulted)
            throw BinarySerializationExceptionFactory.InvalidOperation(
                "AppendRequiresRecovery",
                "继续追加输入之前必须先调用 Reset 或丢弃无效前缀。");
    }
}
