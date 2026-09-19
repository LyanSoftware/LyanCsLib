namespace Lytec.BluetoothLE;

public interface IBleConnection : IAsyncDisposable
{
    string DeviceId { get; }
    IObservable<ConnectionState> StateChanges { get; }
    Task<int> RequestMtuAsync(int mtu, int timeout = 5000, CancellationToken cancelToken = default);
    Task<IBleGattService> GetServiceAsync(Guid serviceUuid, CancellationToken cancelToken = default);
}
