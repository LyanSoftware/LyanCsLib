using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class SCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum CardType : byte
        {
            SCL2008 = 0xff,
            ADSCL2500 = 0,
            ADSCL2800 = 1,
            CHECK2500 = 2,
            ADSCL2900 = 3,
            CHECK2600 = 4,
        }

    }
}
