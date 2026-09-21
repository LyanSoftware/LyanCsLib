using System;
using System.Buffers;
using Lytec.Common.Localization.Extensions;

namespace Lytec.Common.Serialization;

/// <summary>
/// 控制流解码器遇到无效候选帧时的处理方式。
/// </summary>
public enum BinaryResynchronizationMode
{
    /// <summary>
    /// 丢弃解析器建议的前缀；解析器未给出建议时丢弃一个字节，然后继续寻找帧。
    /// </summary>
    Automatic,

    /// <summary>
    /// 在首个无效候选帧处停止并进入故障状态。
    /// </summary>
    StopOnInvalidData,
}

/// <summary>
/// 表示有状态流解码器的生命周期。
/// </summary>
public enum BinaryDecoderState
{
    /// <summary>解码器可以接收新帧。</summary>
    Ready,

    /// <summary>解码器正在保留一个尚未收满的候选帧。</summary>
    Receiving,

    /// <summary>输入流已正常结束。</summary>
    Completed,

    /// <summary>解码器遇到终止性错误，必须复位后才能继续使用。</summary>
    Faulted,
}

/// <summary>
/// 无状态解析器从偏移零检查候选帧后返回的结果。
/// </summary>
public readonly struct BinaryParseResult : IEquatable<BinaryParseResult>
{
    private const string LocalizeScope = "Lytec.Common.Serialization.BinaryParseResult";

    public OperationStatus Status { get; }

    /// <summary>
    /// 当状态为 <see cref="OperationStatus.Done"/> 时表示完整帧长度；当状态为
    /// <see cref="OperationStatus.InvalidData"/> 时表示可安全丢弃的前缀长度建议，
    /// 零表示解析器没有建议；当状态为 <see cref="OperationStatus.NeedMoreData"/>
    /// 时始终为零。
    /// </summary>
    public int Consumed { get; }

    /// <summary>解析器已经检查的源字节数。</summary>
    public int Examined { get; }

    private BinaryParseResult(OperationStatus status, int consumed, int examined)
    {
        if (status != OperationStatus.Done
            && status != OperationStatus.NeedMoreData
            && status != OperationStatus.InvalidData)
            throw new ArgumentOutOfRangeException(nameof(status))
                .Localize(
                    LocalizeScope,
                    "InvalidStatus",
                    "状态 {{Status}} 不能用于二进制解析结果。",
                    ("Status", status));
        if (consumed < 0)
            throw new ArgumentOutOfRangeException(nameof(consumed))
                .Localize(
                    LocalizeScope,
                    "NegativeConsumed",
                    "已消费字节数不能小于零。");
        if (examined < 0)
            throw new ArgumentOutOfRangeException(nameof(examined))
                .Localize(
                    LocalizeScope,
                    "NegativeExamined",
                    "已检查字节数不能小于零。");
        if (status == OperationStatus.NeedMoreData && consumed != 0)
            throw new ArgumentException("NeedMoreData 状态不能消费输入。", nameof(consumed))
                .Localize(
                    LocalizeScope,
                    "NeedMoreDataConsumed",
                    "NeedMoreData 状态不能消费输入。");
        if (consumed > examined)
            throw new ArgumentException("Consumed 不能大于 Examined。", nameof(consumed))
                .Localize(
                    LocalizeScope,
                    "ConsumedExceedsExamined",
                    "已消费字节数不能大于已检查字节数。");

        Status = status;
        Consumed = consumed;
        Examined = examined;
    }

    public static BinaryParseResult Done(int consumed, int examined)
        => new(OperationStatus.Done, consumed, examined);

    public static BinaryParseResult Done(int consumed)
        => Done(consumed, consumed);

    public static BinaryParseResult NeedMoreData(int examined)
        => new(OperationStatus.NeedMoreData, 0, examined);

    public static BinaryParseResult InvalidData(int suggestedDiscard, int examined)
        => new(OperationStatus.InvalidData, suggestedDiscard, examined);

    public bool Equals(BinaryParseResult other)
        => Status == other.Status && Consumed == other.Consumed && Examined == other.Examined;

    public override bool Equals(object? obj) => obj is BinaryParseResult other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Status, Consumed, Examined);

    public static bool operator ==(BinaryParseResult left, BinaryParseResult right) => left.Equals(right);

    public static bool operator !=(BinaryParseResult left, BinaryParseResult right) => !left.Equals(right);

    public override string ToString() => $"{Status}, Consumed = {Consumed}, Examined = {Examined}";
}

/// <summary>
/// 流解码器执行一次追加或完成操作后返回的结果。
/// </summary>
public readonly struct BinaryDecodeResult : IEquatable<BinaryDecodeResult>
{
    private const string LocalizeScope = "Lytec.Common.Serialization.BinaryDecodeResult";

    public OperationStatus Status { get; }

    /// <summary>从本次调用所提供的源数据中消费的字节数。</summary>
    public int Consumed { get; }

    /// <summary>
    /// 本次调用中自动重新同步所丢弃的已缓存或新提供字节数；该值可以大于
    /// <see cref="Consumed"/>。
    /// </summary>
    public int Discarded { get; }

    public BinaryDecodeResult(OperationStatus status, int consumed, int discarded)
    {
        if (status != OperationStatus.Done
            && status != OperationStatus.NeedMoreData
            && status != OperationStatus.InvalidData)
            throw new ArgumentOutOfRangeException(nameof(status))
                .Localize(
                    LocalizeScope,
                    "InvalidStatus",
                    "状态 {{Status}} 不能用于二进制解码结果。",
                    ("Status", status));
        if (consumed < 0)
            throw new ArgumentOutOfRangeException(nameof(consumed))
                .Localize(
                    LocalizeScope,
                    "NegativeConsumed",
                    "已消费字节数不能小于零。");
        if (discarded < 0)
            throw new ArgumentOutOfRangeException(nameof(discarded))
                .Localize(
                    LocalizeScope,
                    "NegativeDiscarded",
                    "已丢弃字节数不能小于零。");

        Status = status;
        Consumed = consumed;
        Discarded = discarded;
    }

    public bool Equals(BinaryDecodeResult other)
        => Status == other.Status && Consumed == other.Consumed && Discarded == other.Discarded;

    public override bool Equals(object? obj) => obj is BinaryDecodeResult other && Equals(other);

    public override int GetHashCode() => HashCode.Combine((int)Status, Consumed, Discarded);

    public static bool operator ==(BinaryDecodeResult left, BinaryDecodeResult right) => left.Equals(right);

    public static bool operator !=(BinaryDecodeResult left, BinaryDecodeResult right) => !left.Equals(right);

    public override string ToString() => $"{Status}, Consumed = {Consumed}, Discarded = {Discarded}";
}
