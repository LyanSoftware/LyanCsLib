using System.Text.RegularExpressions;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    public enum FPGAProgramFileNameTypes
    {
        Unlimited = 0,
        Standard,
        Extended,
        RangeAndPerOnly,
        RangeAndPerOnly_Extended,
        ColorOnly,
        AllInOne = Unlimited
    }

    public static readonly Regex FPGAProgramFileNameRegex_Standard = new Regex(@"^(?<Name>.*?(?<Maker>[ALXG]))(?<Range>[0-7])(?<Mod>[0-4])(?<Per>[0-4])(?<Ext>\..*?)$", RegexOptions.Compiled);
    public static readonly Regex FPGAProgramFileNameRegex_Extended = new Regex(@"^(?<Name>.*?(?<Maker>[ALXG]))(?<Range>[0-5])(?<Mod>[0-4])(?<Per>[0-4])(?<Ext>\..*?)$", RegexOptions.Compiled);
    public static readonly Regex FPGAProgramFileNameRegex_ColorOnly = new Regex(@"^(?<Name>.*?(?<Maker>[ALXG]))(?<ColorType>RG|RGB)(?<Ext>\..*?)$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

}
