using System.ComponentModel;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol.VisMonitor.TsAvs05;

/// <summary>
/// 天星智联TS AVS05 能见度监测仪状态
/// </summary>
[JsonConverter(typeof(StringEnumConverter))]
public enum StatusCode : ushort
{
    [Description("正常")]
    Normal = 0,
    [Description("电源故障")]
    PowerFault = 1,
    [Description("接收器失明")]
    ReceiverBlindness = 2,
    [Description("接收器故障")]
    ReceiverFault = 3,
    [Description("发射器温度故障")]
    EmitterTemperatureFault = 4
}
