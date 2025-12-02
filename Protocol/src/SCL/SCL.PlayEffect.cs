using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

partial class SCL
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum PlayEffect
    {
        Random = 0,
        Static = 1,
        MoveToTop = 2,
        MoveToLeft = 3,
        Dissolving = 4,
        CenterSpreadsOutInAllDirections = 5,
        MoveToBottom = 6,
        MoveToRight = 7,
        HorizontalBlinds = 8,
        VerticalBlinds = 9,
        Flash = 10
    }
}
