using Newtonsoft.Json;

namespace Lytec.Common.Communication
{
    /// <summary>
    /// 连接方式
    /// </summary>
    [Flags]
    [JsonConverter(typeof(Newtonsoft.Json.Converters.StringEnumConverter))]
    public enum ConnectType
    {
        Invalid,
        UART,
        UDP,
        TCP,
        UDPServer,
        TCPServer,
    }
}
