namespace Lytec.BluetoothLE;

/// <summary>
/// 蓝牙访问权限与可用性的状态。
/// </summary>
public enum BleAccessState
{
    /// <summary>
    /// 已授权且蓝牙可用，可以开始扫描或连接。
    /// </summary>
    Available,

    /// <summary>
    /// 权限被用户或系统策略拒绝。
    /// 引导用户前往系统设置授予蓝牙权限。
    /// </summary>
    Denied,

    /// <summary>
    /// 蓝牙未开启（如飞行模式、用户关闭了蓝牙）。
    /// 引导用户打开蓝牙开关。
    /// </summary>
    Disabled,

    /// <summary>
    /// 当前平台或设备不支持 BLE。
    /// 应显示错误信息而非引导用户操作。
    /// </summary>
    NotSupported,

    /// <summary>
    /// 尚未请求权限，或状态无法确定。调用方可以选择重新请求。
    /// </summary>
    Unknown
}
