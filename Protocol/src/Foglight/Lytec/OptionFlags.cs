using Lytec.Common.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol.Foglight;

[JsonConverter(typeof(StringEnumConverter))]
[Serializable]
[Endian(SCL.Constants.DefaultEndian)]
public enum OptionFlags : ushort
{
    /// <summary> 无 </summary>
    None = 0,
    /// <summary> 按电量调整亮度 </summary>
    UseLowPowerActions = 1 << 0,
    /// <summary> 使用亮度传感器自动调整亮度 </summary>
    AutoBright = 1 << 1,
    /// <summary> 使用900MHz频段 </summary>
    Use900MHz = 1 << 2,
    /// <summary> 是上行方向(使用频段的后半部分信道) </summary>
    IsUpward = 1 << 3,
    /// <summary> 文字逆序显示 </summary>
    ReverseText = 1 << 4,
    /// <summary> 回读节点详细信息（下级节点id、电压、电流等） </summary>
    UseDetailedNodeInfo = 1 << 5,
    /// <summary> 启用子节点自动休眠以减少耗电 </summary>
    UseStopMode = 1 << 6,
    /// <summary> 扫描输出作为雾灯使用时, 闪烁时减少点亮的灯数量以减小电流（同时使亮度变低） </summary>
    FLedScanCrossLightInput = 1 << 7,
    /// <summary> 是否使用定时开关 </summary>
    UseTimer = 1 << 8,
}
