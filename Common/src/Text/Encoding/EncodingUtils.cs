using System;
using System.Collections.Generic;
using System.Text;
using SysEncoding = System.Text.Encoding;

namespace Lytec.Common.Text.Encoding
{
    public static class EncodingUtils
    {
        public static bool IsSBCS(this SysEncoding encoding)
        {
            if (encoding.CodePage == SysEncoding.ASCII.CodePage
                || encoding.CodePage == SysEncoding.GetEncoding("ISO-8859-1").CodePage)
                return true;
            else if (encoding.CodePage == SysEncoding.Unicode.CodePage
                || encoding.CodePage == SysEncoding.BigEndianUnicode.CodePage
                || encoding.CodePage == SysEncoding.UTF8.CodePage
                || encoding.CodePage == SysEncoding.UTF32.CodePage)
                return false;
            return encoding.GetType().Name.ToUpper().Contains("SBCS");
        }

        public static bool IsDBCS(this SysEncoding encoding)
        => encoding.GetType().Name.ToUpper().Contains("DBCS");
    }
}
