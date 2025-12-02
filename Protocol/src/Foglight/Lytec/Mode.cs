using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol.Foglight;

[JsonConverter(typeof(StringEnumConverter))]
public enum Mode : ushort
{
    Normal = 0,
    DemoShow = 1,
}
