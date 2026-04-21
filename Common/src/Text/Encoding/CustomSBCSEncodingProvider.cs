using System;
using System.Collections.Generic;
using System.Text;
using SysEncoding = System.Text.Encoding;

namespace Lytec.Common.Text.Encoding
{
    public class CustomSBCSEncodingProvider
    {
        protected abstract class CustomSBCSEncoding<TImpl> : CustomEncoding where TImpl : CustomSBCSEncoding<TImpl>
        {
            public static readonly char[] SpecialCodePoints = new int[]
            {
                0x00, 0x0A, 0x0D, 0x1B, 0x3F, 0x60
            }.Select(c => (char)c).ToArray();

            static CustomSBCSEncoding()
            {
                if (!typeof(TImpl).Name.ToUpper().Contains("SBCS"))
                    throw new InvalidOperationException("The class name for a custom SBCS Encoding must contain \"SBCS\".");
            }

            protected abstract int[] GetTable();
            public int[] TableData { get; }
            public Dictionary<int, byte> Table { get; }

            public CustomSBCSEncoding()
            {
                TableData = GetTable();
                Table = Enumerable.Range(0, 0x100)
                    .Select(d => (byte)d)
                    .Where(d => TableData[d] != 0)
                    .ToDictionary(d => TableData[d], d => d);
            }

            public override IEnumerable<int> ContainsCodePoints => Table.Keys;
            public override bool ContainsCodePoint(int code) => Table.ContainsKey(code);

            public override int GetByteCount(char[] chars, int index, int count)
            {
                count = Math.Min(chars.Length - index, count);
                var ret = 0;
                for (var i = index; i < count; i++)
                {
                    ret++;
                    var ch = chars[i];
                    if (i + 1 < count && char.IsSurrogatePair(ch, chars[i + 1]))
                        i++;
                }
                return ret;
            }

            public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
            {
                var count = Math.Min(chars.Length - charIndex, charCount);
                var ret = 0;
                for (var i = charIndex; i < count; i++)
                {
                    int ch = chars[i];
                    if (i + 1 < count && char.IsSurrogatePair(chars[i], chars[i + 1]))
                    {
                        ch = char.ConvertToUtf32(chars[i], chars[i + 1]);
                        i++;
                    }
                    bytes[byteIndex + ret] = Table.TryGetValue(ch, out var b) ? b : (byte)'?';
                    ret++;
                }
                return ret;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            => bytes.Skip(index)
                .Take(count)
                .Select(b => char.ConvertFromUtf32(TableData[b]).Length)
                .Sum();

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                var count = 0;
                foreach (var c in bytes.Skip(byteIndex).Take(byteCount))
                {
                    var chs = char.ConvertFromUtf32(TableData[c]).ToCharArray();
                    var offset = charIndex + count;
                    var len = Math.Min(chs.Length, chars.Length - offset);
                    Array.Copy(chs, 0, chars, offset, len);
                    count += len;
                }
                return count;
            }

            public override int GetMaxByteCount(int charCount) => 1;

            public override int GetMaxCharCount(int byteCount) => 2;

        }

    }
}
