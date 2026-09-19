using System.Reactive;
using System.Reactive.Linq;
using Shiny.BluetoothLE;
using ShinyScanConfig = Shiny.BluetoothLE.ScanConfig;
using ShinyConnectConfig = Shiny.BluetoothLE.ConnectionConfig;
using Lytec.Common.Localization.Extensions;

namespace Lytec.BluetoothLE.Shiny;

internal class BleService(IBleManager BleManager) : IBleService
{
    private const string LocalizeScope = "Lytec.BluetoothLE";

    public IBleManager BleManager { get; } = BleManager;

    public BleAccessState CurrentAccess => MapAccessState(BleManager.CurrentAccess);

    public IObservable<BleDeviceInfo> Scan(ScanConfig options)
    {
        var obs = BleManager.Scan(new ShinyScanConfig([.. options.ServiceUuids.Select(x => x.ToString())]));
        if (options.DeviceName != null)
            obs = obs.Where(x => x.Peripheral.Name == options.DeviceName);
        return obs.Select(x => new BleDeviceInfo(
            x.Peripheral.Uuid,
            x.Peripheral.Name,
            x.Rssi,
            x.AdvertisementData?.ServiceUuids?.Select(x => Guid.Parse(x)).ToList() ?? []
        ));
    }

    public async Task<IBleConnection> ConnectAsync(string deviceId, ConnectConfig? connectConfig = null, TimeSpan? timeout = null, CancellationToken cancelToken = default)
    {
        if (BleManager.GetKnownPeripheral(deviceId) is IPeripheral p)
        {
            await p.ConnectAsync(new ShinyConnectConfig(connectConfig?.AutoConnect != false), cancelToken, timeout);
            return new BleConnection(p);
        }
        throw new ArgumentException("Unknown Device", nameof(deviceId))
            .Localize(
                LocalizeScope,
                "UnknownDeviceError",
                "Unknown Device"
            );
    }

    public async Task DisconnectAsync(string deviceId, TimeSpan? timeout = null, CancellationToken cancelToken = default)
    {
        if (BleManager.GetConnectedPeripherals()
                .FirstOrDefault(x => x.Uuid == deviceId) is IPeripheral p)
            await p.DisconnectAsync(cancelToken, timeout);
    }

    public async Task<BleAccessState> RequestAccessAsync()
    {
        return MapAccessState(await BleManager.RequestAccessAsync());
    }

    private static BleAccessState MapAccessState(AccessState state)
    {
        return state switch
        {
            AccessState.Available => BleAccessState.Available,
            AccessState.Denied => BleAccessState.Denied,
            AccessState.Disabled => BleAccessState.Disabled,
            AccessState.NotSupported => BleAccessState.NotSupported,
            AccessState.Restricted => BleAccessState.Denied,
            AccessState.NotSetup => BleAccessState.Unknown,
            _ => BleAccessState.Unknown
        };
    }
}
