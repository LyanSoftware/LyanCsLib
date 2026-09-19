namespace Lytec.BluetoothLE;

public record BleDeviceInfo(string Id, string? Name, int Rssi, IReadOnlyList<Guid> ServiceUuids);
