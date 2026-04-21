using System;
using System.Collections.Generic;
using System.Text;
using SysEncoding = System.Text.Encoding;

namespace Lytec.Common.Text.Encoding
{
    public abstract class CustomEncoding : SysEncoding
    {

        public static readonly IReadOnlyDictionary<char, string> ModifierCharFallback
        = new Dictionary<char, string>()
        {
            { '\u0300', "`" },
            { '\u0301', "'" },
            { '\u0302', "^" },
            { '\u0303', "~" },
            { '\u0304', "_" },
            { '\u0305', "=" },
            { '\u0306', "~" },
            { '\u0307', "." },
            { '\u0308', ":" },
            { '\u0309', "?" },
            { '\u030A', "o" },
            { '\u030B', "''" },
            { '\u030C', "v" },
            { '\u030D', "|" },
            { '\u030E', "" },
            { '\u030F', "~~" },
            { '\u0310', "n" },
            { '\u0311', "~" },
            { '\u0312', "," },
            { '\u0313', "h" },
            { '\u0314', ")" },
            { '\u0315', "" },
            { '\u0316', "_" },
            { '\u0317', "_" },
            { '\u0318', "<" },
            { '\u0319', ">" },
            { '\u031A', "^" },
            { '\u031B', "+" },
            { '\u031C', "w" },
            { '\u031D', "^" },
            { '\u031E', "v" },
            { '\u031F', "+" },
            { '\u0320', "-" },
            { '\u0321', "j" },
            { '\u0322', "." },
            { '\u0323', "." },
            { '\u0324', ".." },
            { '\u0325', "_" },
            { '\u0326', "," },
            { '\u0327', "," },
            { '\u0328', ";" },
            { '\u0329', "=" },
            { '\u032A', "[" },
            { '\u032B', "" },
            { '\u032C', "v" },
            { '\u032D', "^" },
            { '\u032E', "~" },
            { '\u032F', "" },
            { '\u0330', "~" },
            { '\u0331', "_" },
            { '\u0332', "_" },
            { '\u0333', "__" },
            { '\u0334', "~" },
            { '\u0335', "-" },
            { '\u0336', "-" },
            { '\u0337', "/" },
            { '\u0338', "/" },
            { '\u0339', "" },
            { '\u033A', "" },
            { '\u033B', "#" },
            { '\u033C', "" },
            { '\u033D', "x" },
            { '\u033E', "~" },
            { '\u033F', "==" },
            { '\u0340', "`" },
            { '\u0341', "'" },
            { '\u0342', "~" },
            { '\u0343', "h" },
            { '\u0344', ":\"" },
            { '\u0345', "i" },
            { '\u0346', "[" },
            { '\u0347', "=" },
            { '\u0348', "||" },
            { '\u0349', "<" },
            { '\u034A', "" },
            { '\u034B', "" },
            { '\u034C', ">" },
            { '\u034D', "w" },
            { '\u034E', "^" },
            { '\u034F', "" },
            { '\u0350', ">" },
            { '\u0351', "" },
            { '\u0352', "" },
            { '\u0353', "x" },
            { '\u0354', "<" },
            { '\u0355', ">" },
            { '\u0356', "" },
            { '\u0357', "" },
            { '\u0358', "" },
            { '\u0359', "*" },
            { '\u035A', "~~" },
            { '\u035B', "" },
            { '\u035C', "_" },
            { '\u035D', "^" },
            { '\u035E', "=" },
            { '\u035F', "_" },
            { '\u0360', "~~" },
            { '\u0361', "^" },
            { '\u0362', ">>" },
        }.ToDictionary(kv => kv.Key, kv => kv.Value.IsNullOrEmpty() ? " " : kv.Value);

        protected abstract string GenericName { get; }

        public override string BodyName => GenericName;
        public override string HeaderName => GenericName;
        public override string WebName => GenericName;
        public override string EncodingName => GenericName;

        public override int CodePage => -1;

        public abstract IEnumerable<int> ContainsCodePoints { get; }
        public abstract bool ContainsCodePoint(int code);

    }
}
