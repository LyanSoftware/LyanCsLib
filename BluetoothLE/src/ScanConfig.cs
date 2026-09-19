namespace Lytec.BluetoothLE;

public class ScanConfig
{
    /// <summary>
    /// 仅扫描广播了这些服务 UUID 之一的设备。
    /// 强烈建议至少指定一个 UUID，以避免扫描到大量无关设备。
    /// </summary>
    public IReadOnlyList<Guid> ServiceUuids { get; init; } = [];

    public string? DeviceName { get; init; }
}
