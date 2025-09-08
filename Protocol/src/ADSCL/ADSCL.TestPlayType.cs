using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TestPlayType : byte
        {
            Default = 0,
            None = 0x55,
            FourHighlightDots = 0xAA,
            DotByDot = 0x3C
        }

    }
}
