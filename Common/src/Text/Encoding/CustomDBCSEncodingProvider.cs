using System;
using System.Collections.Generic;
using System.Text;
using SysEncoding = System.Text.Encoding;

namespace Lytec.Common.Text.Encoding
{
    public class CustomDBCSEncodingProvider
    {
        public const byte DbcsStart = 0x80;

        protected abstract class CustomDBCSEncoding<TImpl> : CustomEncoding where TImpl : CustomDBCSEncoding<TImpl>
        {
            static CustomDBCSEncoding()
            {
                if (!typeof(TImpl).Name.ToUpper().Contains("DBCS"))
                    throw new InvalidOperationException("The class name for a custom DBCS Encoding must contain \"DBCS\".");
            }

            protected abstract IReadOnlyDictionary<int, byte[]> GetEncodeTable();
            protected abstract IReadOnlyDictionary<byte[], int> GetDecodeTable();

            public IReadOnlyDictionary<int, ushort> EncodeTable { get; }
            public IReadOnlyDictionary<ushort, int> DecodeTable { get; }

            public CustomDBCSEncoding()
            {
                var edic = new Dictionary<int, ushort>();
                var ddic = new Dictionary<ushort, int>();

                foreach (var i in Enumerable.Range(0, DbcsStart))
                {
                    edic.Add(i, (byte)i);
                    ddic.Add((byte)i, i);
                }

                foreach (var (k, v) in GetEncodeTable())
                {
                    if (v.Length > 2)
                        throw new InvalidDataException();
                    int ch = v[0];
                    if (v.Length > 1)
                        ch |= v[1] << 8;
                    edic[k] = (ushort)ch;
                }

                foreach (var (k, v) in GetDecodeTable())
                {
                    if (k.Length > 2)
                        throw new InvalidDataException();
                    int ch = k[0];
                    if (k.Length > 1)
                        ch |= k[1] << 8;
                    if (!ddic.ContainsKey((ushort)ch))
                        ddic[(ushort)ch] = v;
                }

                EncodeTable = edic;
                DecodeTable = ddic;
            }

            public override IEnumerable<int> ContainsCodePoints => EncodeTable.Keys;
            public override bool ContainsCodePoint(int code) => EncodeTable.ContainsKey(code);

            public override int GetByteCount(char[] chars, int index, int count)
            {
                count = Math.Min(chars.Length - index, count);
                var ret = 0;
                for (var i = index; i < count; i++)
                {
                    int ch = chars[i];
                    if (i + 1 < count && char.IsSurrogatePair(chars[i], chars[i + 1]))
                    {
                        ch = char.ConvertToUtf32(chars[i], chars[i + 1]);
                        i++;
                    }
                    ret += EncodeTable.TryGetValue(ch, out var v) && v >= DbcsStart ? 2 : 1;
                }
                return ret;
            }

            public static readonly byte[] ReplacementBytesBuf = new byte[] { (byte)'?' };
            public static readonly char[] ReplacementCharsBuf = char.ConvertFromUtf32(Rune.ReplacementChar.Value).ToCharArray();

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
                    byte[] buf;
                    if (EncodeTable.TryGetValue(ch, out var v))
                        buf = v >= DbcsStart ? new byte[] { (byte)(v & 0xFF), (byte)(v >> 8) } : new byte[] { (byte)v };
                    else buf = ReplacementBytesBuf;
                    var offset = byteIndex + ret;
                    var len = Math.Min(buf.Length, bytes.Length - offset);
                    Array.Copy(buf, 0, bytes, offset, len);
                    ret += len;
                }
                return ret;
            }

            public override int GetCharCount(byte[] bytes, int index, int count)
            {
                count = Math.Min(bytes.Length - index, count);
                var ret = 0;
                for (var i = index; i < count; i++)
                {
                    int v = bytes[i];
                    if (v >= DbcsStart)
                    {
                        if (i + 1 < count)
                        {
                            v |= bytes[i + 1] << 8;
                            i++;
                        }
                        else return ret + 1;
                    }
                    ret += DecodeTable.TryGetValue((ushort)v, out var ch) ? char.ConvertFromUtf32(ch).Length : 1;
                }
                return ret;
            }

            public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
            {
                var count = Math.Min(bytes.Length - byteIndex, byteCount);
                var ret = 0;
                for (var i = byteIndex; i < count; i++)
                {
                    int v = bytes[i];
                    if (v >= DbcsStart && i + 1 < count)
                    {
                        v |= bytes[i + 1] << 8;
                        i++;
                    }
                    var chs = DecodeTable.TryGetValue((ushort)v, out var ch) ? char.ConvertFromUtf32(ch).ToCharArray() : ReplacementCharsBuf;
                    var offset = charIndex + ret;
                    var len = Math.Min(chs.Length, chars.Length - offset);
                    Array.Copy(chs, 0, chars, offset, len);
                    ret += len;
                }
                return ret;
            }

            public override int GetMaxByteCount(int charCount) => 1;

            public override int GetMaxCharCount(int byteCount) => 2;
        }
    }
}
