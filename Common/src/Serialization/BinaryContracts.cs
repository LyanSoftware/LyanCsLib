using System;
using System.Buffers;
using Lytec.Common.Data;

namespace Lytec.Common.Serialization;

/// <summary>提供二进制格式在线路上的有限长度边界。</summary>
public interface IBinaryFormat
{
    int MinimumFrameLength { get; }

    int MaximumFrameLength { get; }
}

/// <summary>将值序列化为已经配置的二进制格式。</summary>
public interface IBinarySerializer<in T> : IBinaryFormat
{
    /// <summary>
    /// 验证 <paramref name="value"/> 并取得精确编码长度。只允许返回
    /// <see cref="OperationStatus.Done"/> 或 <see cref="OperationStatus.InvalidData"/>。
    /// </summary>
    OperationStatus TryGetSerializedLength(T value, out int length);

    /// <summary>
    /// 写入一个完整值。失败时必须保持 <paramref name="destination"/> 不变，
    /// 并把 <paramref name="written"/> 设为零。
    /// </summary>
    OperationStatus TrySerialize(T value, Span<byte> destination, out int written);
}

/// <summary>
/// 从偏移零开始解析一个候选帧。解析器无状态；其实现不可变时可以并发复用。
/// </summary>
public interface IBinaryFrameParser<T> : IBinaryFormat
{
    /// <param name="isFinalBlock">
    /// 为 true 表示不会再追加字节。符合约定的解析器不得对最终块返回
    /// <see cref="OperationStatus.NeedMoreData"/>。
    /// </param>
    BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out T value);
}

/// <summary>为一种格式提供无状态和流式反序列化。</summary>
public interface IBinaryDeserializer<T> : IBinaryFrameParser<T>
{
    BinaryResynchronizationMode ResynchronizationMode { get; }

    IBinaryStreamDecoder<T> CreateStreamDecoder();
}

/// <summary>组合一种已配置格式的序列化与反序列化能力。</summary>
public interface IBinaryCodec<T> : IBinarySerializer<T>, IBinaryDeserializer<T>
{
}

/// <summary>声明线路大小固定的编解码器。</summary>
public interface IFixedBinaryCodec<T> : IBinaryCodec<T>
{
    int FixedSize { get; }
}

/// <summary>
/// 为固定布局值生成的标记与便捷接口。
/// </summary>
public interface IBinarySerializable
{
    int SerializedSize { get; }

    byte[] Serialize(Endian? endian = null);

    OperationStatus TrySerialize(
        Span<byte> destination,
        out int written,
        Endian? endian = null);
}

/// <summary>填充固定长度解码器预览中缺少的全部字节。</summary>
public delegate bool TryFillMissingBytes(int offset, Span<byte> destination);

/// <summary>生成的固定长度解码器所提供的附加能力。</summary>
public interface IFixedBinaryStreamDecoder<T> : IBinaryStreamDecoder<T>
{
    int RemainingLength { get; }

    OperationStatus TryGetTemporary(TryFillMissingBytes fillMissing, out T value);

    bool TryDiscardOldest(int count);
}
