using Shiny.BluetoothLE;

namespace Lytec.BluetoothLE.Shiny;

internal record BleGattService(BleConnection Connection, Guid Uuid) : IBleGattService
{
    public BleConnection Connection { get; } = Connection;
    public Guid Uuid { get; } = Uuid;
    public IPeripheral Peripheral => Connection.Peripheral;
    readonly string UuidStr = Uuid.ToString();

    public async Task<IBleCharacteristic> GetCharacteristicAsync(Guid characteristicUuid, CancellationToken cancelToken = default)
    {
        var c = await Peripheral.GetCharacteristicAsync(UuidStr, characteristicUuid.ToString(), cancelToken);
        return new BleCharacteristic(this, c, Guid.Parse(c.Uuid));
    }

}
