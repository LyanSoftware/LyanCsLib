using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [Serializable]
        [JsonConverter(typeof(StringEnumConverter))]
        public enum DiskDriver : byte
        {
            A = 0,
            B = 1,
            C = 2,
            Flash = A,
            SDCard = B,
            MemoryDisk = C,
        }

    }
}
