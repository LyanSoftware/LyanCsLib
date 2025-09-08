using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum SpStructIndex
        {
            RuntimeInfo = 0,
            AllConfigs = 1,
            FPGARAM_1stPart = 2,
            FPGARAM_2ndPart = 3,
        }

    }
}
