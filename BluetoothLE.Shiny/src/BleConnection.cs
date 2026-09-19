using System.Reactive.Linq;
using Shiny.BluetoothLE;
using ShinyConnectionState = Shiny.BluetoothLE.ConnectionState;

namespace Lytec.BluetoothLE.Shiny;

internal class BleConnection(IPeripheral Peripheral) : IBleConnection
{
    public IPeripheral Peripheral { get; } = Peripheral;

    public string DeviceId => Peripheral.Uuid;

    public IObservable<ConnectionState> StateChanges => Peripheral.WhenStatusChanged().Select(x => x switch
    {
        ShinyConnectionState.Disconnected => ConnectionState.Disconnected,
        ShinyConnectionState.Disconnecting => ConnectionState.Disconnecting,
        ShinyConnectionState.Connected => ConnectionState.Connected,
        ShinyConnectionState.Connecting => ConnectionState.Connecting,
        _ => ConnectionState.Unknown,
    });

    public async ValueTask DisposeAsync()
    {
        await Peripheral.DisconnectAsync();
        GC.SuppressFinalize(this);
    }

    public async Task<IBleGattService> GetServiceAsync(Guid serviceUuid, CancellationToken cancelToken = default)
    {
        var service = await Peripheral.GetServiceAsync(serviceUuid.ToString(), cancelToken);
        return new BleGattService(this, Guid.Parse(service.Uuid));
    }

    public Task<int> RequestMtuAsync(int mtu, int timeout = 5000, CancellationToken cancelToken = default)
    => Peripheral.TryRequestMtuAsync(mtu, timeout, cancelToken);
}
