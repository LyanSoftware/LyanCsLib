using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

partial class SCL
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum TcpProtocol : byte
    {
        /// <summary> 禁用TCP </summary>
        Disabled = 0xFF,
        /// <summary> 默认SCL/ADSCL协议 </summary>
        SCL = 0,
    }
}
