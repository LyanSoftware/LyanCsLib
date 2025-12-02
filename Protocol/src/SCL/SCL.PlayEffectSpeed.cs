using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

partial class SCL
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PlayEffectSpeed
    {
        Slowest = 1,
        Slower2 = 2,
        Slower1 = 3,
        Normal = 4,
        Faster1 = 5,
        Faster2 = 6,
        Fastest = 7
    }
}
