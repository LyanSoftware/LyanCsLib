namespace Lytec.BluetoothLE;

/// <summary>
/// Provides a predictable BLE service on platforms where BLE is unavailable.
/// </summary>
public sealed class UnsupportedBleService : IBleService
{
    /// <inheritdoc />
    public BleAccessState CurrentAccess => BleAccessState.NotSupported;

    /// <inheritdoc />
    public Task<BleAccessState> RequestAccessAsync()
        => Task.FromResult(BleAccessState.NotSupported);

    /// <inheritdoc />
    public IObservable<BleDeviceInfo> Scan(ScanConfig options)
        => throw CreateException();

    /// <inheritdoc />
    public Task<IBleConnection> ConnectAsync(
        string deviceId,
        ConnectConfig? connectConfig = null,
        TimeSpan? timeout = null,
        CancellationToken cancelToken = default)
        => Task.FromException<IBleConnection>(CreateException());

    /// <inheritdoc />
    public Task DisconnectAsync(
        string deviceId,
        TimeSpan? timeout = null,
        CancellationToken cancelToken = default)
        => Task.FromException(CreateException());

    private static PlatformNotSupportedException CreateException()
        => new("Bluetooth Low Energy is not supported on this operating system.");
}
