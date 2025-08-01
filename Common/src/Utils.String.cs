using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace Lytec.Common
{
    partial class Utils
    {
        /// <summary>
        /// byte[]转Hex-string
        /// </summary>
        /// <param name="data"></param>
        /// <param name="sep">字节分隔</param>
        /// <returns></returns>
        public static string ToHex(this byte[] data, string sep = " ")
        => sep == "-" ? BitConverter.ToString(data) : BitConverter.ToString(data).Replace("-", sep);

        public static int ChrToInt(char chr)
        {
            if (chr >= '0' && chr <= '9')
                return chr - '0';
            else if (chr >= 'a' && chr <= 'f')
                return chr - 'a' + 10;
            else if (chr >= 'A' && chr <= 'F')
                return chr - 'A' + 10;
            else throw new InvalidCastException();
        }

        /// <summary>
        /// Hex-string转byte[]
        /// </summary>
        /// <param name="str"></param>
        /// <param name="sepPattern">字节分隔</param>
        /// <returns></returns>
        public static byte[] HexToByteArray(this string str, string sepPattern = " ")
        {
            str = Regex.Replace(str, sepPattern, "");
            if ((str.Length % 2) != 0)
                throw new InvalidCastException();
            var data = new List<byte>(str.Length / 2);
            var chars = str.ToCharArray();
            for (int i = 0; ; i += 2)
            {
                while (i < chars.Length && (chars[i] == '\r' || chars[i] == '\n'))
                    i++;
                if (i >= chars.Length)
                    break;
                data.Add((byte)((ChrToInt(chars[i]) << 4) | ChrToInt(chars[i + 1])));
            }
            return data.ToArray();
        }

    }
}
