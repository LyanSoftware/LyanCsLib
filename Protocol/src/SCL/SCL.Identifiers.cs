using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.ComponentModel;

namespace Lytec.Protocol;

public static partial class SCL
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum SCLType : byte
    {
        [Description("无效")]
        Invalid = 0xff,
        SuperComm = 0,
        SCL2008 = 1
    }

    [JsonConverter(typeof(StringEnumConverter))]
    public enum InitType
    {
        Invalid = 0,
        SCL2008,
        SCL2008A,
        SCLCheck,
        ADSCL2500,
        ADSCL2800,
    }

    /// <summary>
    /// 不超过7字节的卡类型识别符
    /// </summary>
    public static readonly IReadOnlyDictionary<InitType, string> CardTypeStrings = new Dictionary<InitType, string>()
    {
        { InitType.SCL2008, "SCL2008" },
        { InitType.SCL2008A, "SCL2008" },
        { InitType.SCLCheck, "CHECK25" },
        { InitType.ADSCL2500, "ADSL250" },
        { InitType.ADSCL2800, "ADSL280" },
    };

    [JsonConverter(typeof(StringEnumConverter))]
    public enum SCLExType
    {
        [Description("无效")]
        Invalid = -1,
        [Description("标准设备")]
        Normal = 0,
        [Description("雾灯设备")]
        FogLight = 1,
    }

}
