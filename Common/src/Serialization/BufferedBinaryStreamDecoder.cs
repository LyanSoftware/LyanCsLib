using System;
using System.Buffers;
using Lytec.Common.Localization.Extensions;

namespace Lytec.Common.Serialization;

/// <summary>
/// 面向无状态帧解析器的默认托管缓冲流解码器。使用字节填充或其他增量转换的协议
/// 可以提供专用解码器。
/// </summary>
public sealed class BufferedBinaryStreamDecoder<T> : IBinaryStreamDecoder<T>
{
    private const string LocalizeScope = "Lytec.Common.Serialization.BufferedBinaryStreamDecoder";

    private readonly IBinaryFrameParser<T> _parser;
    private readonly BinaryResynchronizationMode _resynchronizationMode;
    private byte[] _buffer;
    private int _bufferedLength;

    public BinaryDecoderState State { get; private set; }

    public int BufferedLength => _bufferedLength;

    public BufferedBinaryStreamDecoder(
        IBinaryFrameParser<T> parser,
        BinaryResynchronizationMode resynchronizationMode)
    {
        ArgumentNullException.ThrowIfNull(parser);
        _parser = parser;
        BinarySerializerExtensions.ValidateBounds(parser);
        if (resynchronizationMode != BinaryResynchronizationMode.Automatic
            && resynchronizationMode != BinaryResynchronizationMode.StopOnInvalidData)
            throw new ArgumentOutOfRangeException(nameof(resynchronizationMode))
                .Localize(
                    LocalizeScope,
                    "InvalidResynchronizationMode",
                    "重新同步模式 {Mode} 无效。",
                    ("Mode", resynchronizationMode));

        _resynchronizationMode = resynchronizationMode;
        _buffer = Array.Empty<byte>();
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

        var oldBufferedLength = _bufferedLength;
        var copiedFromSource = 0;
        var removedFromCombined = 0;
        var discarded = 0;

        while (true)
        {
            if (copiedFromSource < source.Length && _bufferedLength < _parser.MaximumFrameLength)
            {
                var copyLength = Math.Min(
                    source.Length - copiedFromSource,
                    _parser.MaximumFrameLength - _bufferedLength);
                EnsureCapacity(checked(_bufferedLength + copyLength));
                source.Slice(copiedFromSource, copyLength).CopyTo(_buffer.AsSpan(_bufferedLength));
                copiedFromSource += copyLength;
                _bufferedLength += copyLength;
            }

            if (_bufferedLength == 0)
            {
                State = BinaryDecoderState.Ready;
                return new BinaryDecodeResult(OperationStatus.NeedMoreData, source.Length, discarded);
            }

            var parsed = _parser.Parse(_buffer.AsSpan(0, _bufferedLength), false, out var parsedValue);
            BinarySerializerExtensions.ValidateParseResult(parsed, _bufferedLength, false);

            switch (parsed.Status)
            {
                case OperationStatus.Done:
                    if (parsed.Consumed < _parser.MinimumFrameLength
                        || parsed.Consumed > _parser.MaximumFrameLength)
                        throw new InvalidOperationException("解析器完成的帧超出其声明边界。")
                            .Localize(
                                LocalizeScope,
                                "CompletedFrameOutOfBounds",
                                "解析器完成的帧超出其声明边界。");

                    var endInCombined = checked(removedFromCombined + parsed.Consumed);
                    var consumedFromSource = Math.Min(
                        source.Length,
                        Math.Max(0, endInCombined - oldBufferedLength));
                    result = parsedValue;
                    _bufferedLength = 0;
                    State = BinaryDecoderState.Ready;
                    return new BinaryDecodeResult(OperationStatus.Done, consumedFromSource, discarded);

                case OperationStatus.NeedMoreData:
                    if (_bufferedLength < _parser.MaximumFrameLength
                        && copiedFromSource == source.Length)
                    {
                        State = BinaryDecoderState.Receiving;
                        return new BinaryDecodeResult(OperationStatus.NeedMoreData, source.Length, discarded);
                    }

                    // 候选帧已经达到有限上限，因此不可能再成为合法的有界帧。
                    if (_resynchronizationMode == BinaryResynchronizationMode.StopOnInvalidData)
                    {
                        State = BinaryDecoderState.Faulted;
                        return new BinaryDecodeResult(OperationStatus.InvalidData, 0, discarded);
                    }

                    DiscardPrefix(1);
                    removedFromCombined++;
                    discarded++;
                    break;

                case OperationStatus.InvalidData:
                    if (_resynchronizationMode == BinaryResynchronizationMode.StopOnInvalidData)
                    {
                        State = BinaryDecoderState.Faulted;
                        return new BinaryDecodeResult(OperationStatus.InvalidData, 0, discarded);
                    }

                    var discard = parsed.Consumed == 0 ? 1 : parsed.Consumed;
                    if (discard > _bufferedLength)
                        throw new InvalidOperationException("解析器建议丢弃的字节数超过了所提供的源数据。")
                            .Localize(
                                LocalizeScope,
                                "DiscardBeyondSource",
                                "解析器建议丢弃的字节数超过了所提供的源数据。");
                    DiscardPrefix(discard);
                    removedFromCombined = checked(removedFromCombined + discard);
                    discarded = checked(discarded + discard);
                    break;

                default:
                    throw new InvalidOperationException($"解析器返回了不允许的状态 {parsed.Status}。")
                        .Localize(
                            LocalizeScope,
                            "InvalidParserStatus",
                            "解析器返回了不允许的状态 {Status}。",
                            ("Status", parsed.Status));
            }
        }

    }

    public BinaryDecodeResult Complete()
    {
        if (State == BinaryDecoderState.Completed || State == BinaryDecoderState.Faulted)
            throw new InvalidOperationException("再次完成解码器之前必须先调用 Reset。")
                .Localize(
                    LocalizeScope,
                    "CompleteRequiresReset",
                    "再次完成解码器之前必须先调用 Reset。");

        if (_bufferedLength == 0)
        {
            State = BinaryDecoderState.Completed;
            return new BinaryDecodeResult(OperationStatus.Done, 0, 0);
        }

        // 完整帧已经由 Append 返回；EOF 明确不作为帧分隔符，因此任何残留候选均为截断帧。
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
        if (State == BinaryDecoderState.Completed || State == BinaryDecoderState.Faulted)
            throw new InvalidOperationException("继续追加输入之前必须先调用 Reset。")
                .Localize(
                    LocalizeScope,
                    "AppendRequiresReset",
                    "继续追加输入之前必须先调用 Reset。");
    }

    private void EnsureCapacity(int required)
    {
        if (required > _parser.MaximumFrameLength)
            throw new InvalidOperationException("解码器缓冲区增长超过 MaximumFrameLength。")
                .Localize(
                    LocalizeScope,
                    "BufferExceedsMaximum",
                    "解码器缓冲区增长超过 MaximumFrameLength。");
        var capacityRequired = required;
        if (_buffer.Length >= capacityRequired)
            return;

        var capacity = _buffer.Length == 0 ? _parser.MinimumFrameLength : _buffer.Length;
        while (capacity < capacityRequired)
        {
            var doubled = capacity <= (int.MaxValue / 2) ? capacity * 2 : int.MaxValue;
            capacity = Math.Min(_parser.MaximumFrameLength, Math.Max(capacityRequired, doubled));
        }
        Array.Resize(ref _buffer, capacity);
    }

    private void DiscardPrefix(int count)
    {
        var remaining = _bufferedLength - count;
        if (remaining > 0)
            Buffer.BlockCopy(_buffer, count, _buffer, 0, remaining);
        _bufferedLength = remaining;
    }
}
