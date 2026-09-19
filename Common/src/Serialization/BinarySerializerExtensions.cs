using System;
using System.Buffers;
using Lytec.Common.Localization.Extensions;

namespace Lytec.Common.Serialization;

public static class BinarySerializerExtensions
{
    /// <summary>把一个值序列化到新分配的精确长度数组中。</summary>
    public static byte[] Serialize<T>(this IBinarySerializer<T> serializer, T value)
    {
        ArgumentNullException.ThrowIfNull(serializer);

        var lengthStatus = serializer.TryGetSerializedLength(value, out var length);
        ValidateMeasuredLength(serializer, lengthStatus, length);

        var result = new byte[length];
        var status = serializer.TrySerialize(value, result, out var written);
        if (status != OperationStatus.Done || written != length)
            throw CreateSerializationException(status, length, written);
        return result;
    }

    /// <summary>把一个值序列化到 <see cref="IBufferWriter{T}"/>。</summary>
    public static void Serialize<T>(
        this IBinarySerializer<T> serializer,
        T value,
        IBufferWriter<byte> destination)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(destination);

        var lengthStatus = serializer.TryGetSerializedLength(value, out var length);
        ValidateMeasuredLength(serializer, lengthStatus, length);

        var span = destination.GetSpan(length).Slice(0, length);
        var status = serializer.TrySerialize(value, span, out var written);
        if (status != OperationStatus.Done || written != length)
            throw CreateSerializationException(status, length, written);
        destination.Advance(written);
    }

    /// <summary>
    /// 从偏移零解析一个有边界的最终输入块。该方法不会搜索其他起点，
    /// 并会把 NeedMoreData 转换为 InvalidData。
    /// </summary>
    public static BinaryParseResult ParseDatagram<T>(
        this IBinaryFrameParser<T> parser,
        ReadOnlySpan<byte> source,
        out T value)
    {
        ArgumentNullException.ThrowIfNull(parser);

        ValidateBounds(parser);
        var result = parser.Parse(source, true, out value);
        ValidateParseResult(result, source.Length, true);
        if (result.Status != OperationStatus.NeedMoreData)
            return result;

        value = default!;
        return BinaryParseResult.InvalidData(0, result.Examined);
    }

    internal static void ValidateBounds(IBinaryFormat format)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(format.MinimumFrameLength, nameof(format));
        ArgumentOutOfRangeException.ThrowIfLessThan(format.MaximumFrameLength, format.MinimumFrameLength, nameof(format));
        ArgumentOutOfRangeException.ThrowIfEqual(format.MaximumFrameLength, int.MaxValue, nameof(format));
    }

    private const string ParserExceptionLocalizeScope = "Lytec.Common.Serialization.BinaryParser";

    internal static void ValidateParseResult(BinaryParseResult result, int sourceLength, bool isFinalBlock)
    {
        if (result.Consumed > sourceLength)
            throw new InvalidOperationException("解析器消费的字节数超过了所提供的源数据。")
                .Localize(
                    ParserExceptionLocalizeScope,
                    "ConsumedBeyondSource",
                    "解析器消费的字节数超过了所提供的源数据。");
        if (result.Examined > sourceLength)
            throw new InvalidOperationException("解析器检查的字节数超过了所提供的源数据。")
                .Localize(
                    ParserExceptionLocalizeScope,
                    "ExaminedBeyondSource",
                    "解析器检查的字节数超过了所提供的源数据。");
        if (isFinalBlock && result.Status == OperationStatus.NeedMoreData)
            return;
        if (result.Status == OperationStatus.Done && result.Consumed == 0)
            throw new InvalidOperationException("完整帧必须至少消费一个字节。")
                .Localize(
                    ParserExceptionLocalizeScope,
                    "EmptyCompletedFrame",
                    "完整帧必须至少消费一个字节。");
    }

    private const string SerializerExceptionLocalizeScope = "Lytec.Common.Serialization.BinarySerializer";

    private static void ValidateMeasuredLength<T>(
        IBinarySerializer<T> serializer,
        OperationStatus status,
        int length)
    {
        ValidateBounds(serializer);
        switch (status)
        {
            case OperationStatus.Done:
                if (length < serializer.MinimumFrameLength || length > serializer.MaximumFrameLength)
                    throw new InvalidOperationException("序列化器返回的长度超出其声明边界。")
                        .Localize(
                            SerializerExceptionLocalizeScope,
                            "LengthOutOfBounds",
                            "序列化器返回的长度超出其声明边界。");
                return;
            case OperationStatus.InvalidData:
                if (length != 0)
                    throw new InvalidOperationException("InvalidData 状态必须报告零序列化长度。")
                        .Localize(
                            SerializerExceptionLocalizeScope,
                            "InvalidDataWithLength",
                            "InvalidData 状态必须报告零序列化长度。");
                throw new InvalidOperationException("该值无法由此二进制序列化器表示。")
                    .Localize(
                        SerializerExceptionLocalizeScope,
                        "ValueNotRepresentable",
                        "该值无法由此二进制序列化器表示。");
            default:
                throw new InvalidOperationException($"TryGetSerializedLength 返回了不允许的状态 {status}。")
                    .Localize(
                        SerializerExceptionLocalizeScope,
                        "InvalidLengthStatus",
                        "TryGetSerializedLength 返回了不允许的状态 {Status}。",
                        ("Status", status));
        }
    }

    private static Exception CreateSerializationException(OperationStatus status, int expected, int written)
        => new InvalidOperationException(
                $"TrySerialize 违反了预先测量的序列化约定。" +
                $"状态 = {status}，预期长度 = {expected}，实际写入 = {written}。")
            .Localize(
                SerializerExceptionLocalizeScope,
                "SerializationContractViolation",
                "TrySerialize 违反了预先测量的序列化约定。状态 = {Status}，预期长度 = {Expected}，实际写入 = {Written}。",
                ("Status", status),
                ("Expected", expected),
                ("Written", written));
}
