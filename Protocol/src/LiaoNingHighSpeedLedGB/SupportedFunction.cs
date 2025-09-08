using System.ComponentModel;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public enum SupportedFunction
{
    [Description("重启设备")]
    Reboot = 1,
    [Description("设备校时")]
    DateSet = 2,
    [Description("亮度调节")]
    LightControl = 3,
    [Description("获取设备状态")]
    QueryStatus = 4,
    [Description("屏幕开关")]
    ControlSwitch = 5,
    [Description("获取屏幕分辨率")]
    QueryResolution = 6,
    [Description("截图")]
    ScreenShot = 7,
    [Description("重置密钥")]
    ResetAppIdAndSecret = 8,
    [Description("获取坏点数量")]
    CountPixelError = 9,
    [Description("文件删除")]
    DeleteDeviceFile = 10,
    [Description("文件下载")]
    DownDeviceFile = 12,
    [Description("文件上传")]
    UploadDeviceFile = 13,
    [Description("获取当前播放截图")]
    QueryScreenShot = 14,
    [Description("获取当前播放节目内容")]
    QueryCurrentPlaylist = 15,
    [Description("获取故障信息")]
    QueryFaultInfo = 16,
    [Description("获取坏点数据")]
    QueryPixelError = 17,
    [Description("断网后恢复为默认内容时间设置")]
    NetRestartTime = 18,
}
