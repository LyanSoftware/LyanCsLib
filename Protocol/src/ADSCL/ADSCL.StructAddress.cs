using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public enum StructAddress
        {
            LoadFromSpStructs = 0,
            FPGAProgram = 0x080000,
            FPGAProgram_STM = 0x08000,
            PlayProgram = 0x0B8000,
            PlayProgram_STM = 0x40000,
            LEDConfig = 0x0E0000,
            FPGARAM = 0x0E1000,
            MacNetConfig = 0x0E2000,
            BIOS = 0x0F8000
        }

    }
}
