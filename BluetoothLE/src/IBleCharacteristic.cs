namespace Lytec.BluetoothLE;

public interface IBleCharacteristic
{
    Guid Uuid { get; }
    Task<byte[]> ReadAsync(int timeout = 3000, CancellationToken cancelToken = default);
    Task WriteAsync(byte[] data, bool withResponse = true, int timeout = 3000, CancellationToken cancelToken = default);
    IObservable<byte[]> Subscribe();
}
