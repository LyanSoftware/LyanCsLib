using System;

namespace Lytec.Common.Serialization;

/// <summary>
/// 用于连续字节流的有状态、非线程安全解码器。
/// </summary>
public interface IBinaryStreamDecoder<T>
{
    BinaryDecoderState State { get; }

    int BufferedLength { get; }

    BinaryDecodeResult Append(byte value, out T result);

    /// <summary>
    /// 最多消费至第一帧结束。该帧之后的字节仍由调用方持有，必须再次提交。
    /// </summary>
    BinaryDecodeResult Append(ReadOnlySpan<byte> source, out T result);

    /// <summary>
    /// 将流标记为已经结束。仅在不存在未完成候选帧时返回 Done；该操作不产生值。
    /// </summary>
    BinaryDecodeResult Complete();

    void Reset();
}
