using Shiny.BluetoothLE;
using System.Reactive.Linq;

namespace Lytec.BluetoothLE.Shiny;

internal class BleCharacteristic(BleGattService Service, BleCharacteristicInfo Info, Guid Uuid) : IBleCharacteristic
{
    public BleGattService Service { get; } = Service;
    public BleCharacteristicInfo Info { get; } = Info;
    public Guid Uuid { get; } = Uuid;

    public IPeripheral Peripheral => Service.Peripheral;

    public async Task<byte[]> ReadAsync(int timeout = 3000, CancellationToken cancelToken = default)
    {
        return (await Peripheral.ReadCharacteristicAsync(Info, cancelToken, timeout)).Data ?? [];
    }

    public IObservable<byte[]> Subscribe() => Peripheral.NotifyCharacteristic(Info).Select(x => x.Data ?? []);

    public Task WriteAsync(byte[] data, bool withResponse = true, int timeout = 3000, CancellationToken cancelToken = default)
    => Peripheral.WriteCharacteristicAsync(Info, data, withResponse, cancelToken, timeout);
}
