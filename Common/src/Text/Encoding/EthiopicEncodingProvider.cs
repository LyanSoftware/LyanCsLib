using System;
using System.Collections.Generic;
using System.Text;
using SysEncoding = System.Text.Encoding;

namespace Lytec.Common.Text.Encoding
{
    public class EthiopicEncodingProvider : CustomDBCSEncodingProvider
    {
        internal static Range CodeToRange(int code) => new Range(code, code + 1);
        internal static readonly Range[] CodeRanges = new Range[]
        {
            // 吉兹字母
            new Range(0x1200, 0x13A0),
            new Range(0x2D80, 0x2DE0),
            new Range(0xAB00, 0xAB30),
            new Range(0x1E7E0, 0x1E800),
            // 拉丁转写（预组合字符）
            CodeToRange('Ḥ'), CodeToRange('ḥ'),
            CodeToRange('Ḫ'), CodeToRange('ḫ'),
            CodeToRange('Ś'), CodeToRange('ś'),
            CodeToRange('Ṣ'), CodeToRange('ṣ'),
            CodeToRange('Ḍ'), CodeToRange('ḍ'),
            CodeToRange('Ḳ'), CodeToRange('ḳ'),
            CodeToRange('Ṭ'), CodeToRange('ṭ'),
            CodeToRange('Ä'), CodeToRange('ä'),
            CodeToRange('ǝ'), CodeToRange('Ǝ'),
            CodeToRange('ə'), CodeToRange('Ə'),
            CodeToRange('ɨ'),
            CodeToRange('ʾ'),
        };

        public static readonly IReadOnlyDictionary<int, byte[]> EncodeTable;
        public static readonly IReadOnlyDictionary<byte[], int> DecodeTable;

        public static readonly SysEncoding Instance;

        static EthiopicEncodingProvider()
        {
            const byte start = 0x81;
            const byte end = 0xFE;
            var b1 = start;
            var b2 = start;
            var enc = new Dictionary<int, byte[]>();
            var dec = new Dictionary<byte[], int>();
            bool add(int code, byte[] data)
            {
                if (b1 > end)
                    return false;
                if (data.Length > 2)
                    throw new InvalidDataException();
                enc.Add(code, data);
                dec.Add(data, code);
                b2++;
                if (b2 > end)
                {
                    b2 = start;
                    b1++;
                    if (b1 > end)
                        return false;
                }
                return true;
            }
            foreach (var code in CodeRanges.SelectMany(r => r.AsEnumerable()).ToList())
                if (!add(code, new byte[] { b1, b2 }))
                    break;
            foreach (var (code, str) in CustomEncoding.ModifierCharFallback)
                if (!add(code, SysEncoding.ASCII.GetBytes(str)))
                    break;
            EncodeTable = enc;
            DecodeTable = dec;
            Instance = new DBCSEthiopicEncoding();
        }

        private class DBCSEthiopicEncoding : CustomDBCSEncoding<DBCSEthiopicEncoding>
        {
            protected override string GenericName => "x-geez-custom";
            public override string EncodingName => "Geez script (Custom DBCS)";

            protected override IReadOnlyDictionary<int, byte[]> GetEncodeTable() => EthiopicEncodingProvider.EncodeTable;

            protected override IReadOnlyDictionary<byte[], int> GetDecodeTable() => EthiopicEncodingProvider.DecodeTable;
        }

    }
}
