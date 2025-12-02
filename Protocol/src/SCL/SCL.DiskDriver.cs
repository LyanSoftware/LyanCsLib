using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum DiskDriver : byte
    {
        A_Flash = 0,
        B_SDCard = 1,
        C_Ram = 2
    }
}
