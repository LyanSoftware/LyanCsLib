namespace Lytec.BluetoothLE;

public interface IBleGattService
{
    Guid Uuid { get; }
    Task<IBleCharacteristic> GetCharacteristicAsync(Guid characteristicUuid, CancellationToken cancelToken = default);
}
