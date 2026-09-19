namespace Lytec.BluetoothLE;

public interface IBleService
{
    IObservable<BleDeviceInfo> Scan(ScanConfig options);

    Task<IBleConnection> ConnectAsync(string deviceId, ConnectConfig? connectConfig = null, TimeSpan? timeout = null, CancellationToken cancelToken = default);
    Task DisconnectAsync(string deviceId, TimeSpan? timeout = null, CancellationToken cancelToken = default);

    Task<BleAccessState> RequestAccessAsync();
    BleAccessState CurrentAccess { get; }
}
