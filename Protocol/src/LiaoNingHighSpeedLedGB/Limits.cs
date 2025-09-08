using System.Text.RegularExpressions;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public static class Limits
{
    public const int AppIdMaxLength = 64;
    public const int SecretMaxLength = 64;
    public static readonly Regex AppIdRegex = new Regex(@$"^[a-zA-Z0-9\-_]{{0,{AppIdMaxLength}}}$", RegexOptions.Compiled);
    public static readonly Regex SecretRegex = new Regex(@$"^[a-zA-Z0-9\-_]{{0,{SecretMaxLength}}}$", RegexOptions.Compiled);
}
