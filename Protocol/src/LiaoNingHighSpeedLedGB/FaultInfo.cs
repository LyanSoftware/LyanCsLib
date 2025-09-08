using Newtonsoft.Json;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

[Serializable]
[JsonObject]
public record FaultInfo(
    int temperature,
    int internalFault,
    int moduleFault,
    int powerFault,
    int pixelFault,
    int checkSystemFault,
    int acPowerFault,
    int spdFault,
    int photoSensitive,
    int doorOpenFault
    )
{
    public bool IsTemperatureFault => temperature != 0;
    public bool IsInternalFault => internalFault != 0;
    public bool IsModuleFault => moduleFault != 0;
    public bool IsPowerFault => powerFault != 0;
    public bool IsPixelFault => pixelFault != 0;
    public bool IsCheckSystemFault => checkSystemFault != 0;
    public bool IsACPowerFault => acPowerFault != 0;
    public bool IsSurgeProtectionFault => spdFault != 0;
    public bool IsBrightnessSensorFault => photoSensitive != 0;
    public bool IsDoorOpenFault => doorOpenFault != 0;
}
