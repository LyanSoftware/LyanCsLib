using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;

namespace Lytec.Protocol
{
    public static partial class ADSCL
    {
        public const Endian DefaultEndian = Endian.Little;
        public static Encoding DefaultEncode { get; set; } = Encoding.GetEncoding(936);

        public const int FalseValue = -1;
        public const string NewLine = "\r\n";
        public const int NameSize = 16;
        public const int MaxDataLength = 1024;
        public const int GBufferSize = 2 * 1024 * 1024;
        public const string ConfigFileName = "CONFIG.LY";
        public const string OtherConfigFileName = "SYSTEM.LY";
        public const string PlaylistFileName = "PLAYLIST.LY";

        public const string SuPw = "LyTeClYtEc";

        public const int FlashWriteMaxFileSeconds = 50; // Flash写入最大文件所需时间
        public const int FlashReadMaxFileSeconds = 24; // Flash读取最大文件所需时间（暂停播放时为16秒）
        public const int FlashWriteBytesPerSecond = GBufferSize / FlashWriteMaxFileSeconds; // Flash写入速度
        public const int FlashReadBytesPerSecond = GBufferSize / FlashReadMaxFileSeconds; // Flash读取速度

        public record AllConfigs(LEDConfig Led, NetConfig Net)
        {
            public const int SizeConst = LEDConfig.SizeConst + NetConfig.SizeConst;
        }

        public static byte[] InitFlashDataBlock(int size, byte fillByte = 0xFF) => GetFlashDataBlock(size, fillByte).ToArray();
        public static IEnumerable<byte> GetFlashDataBlock(int size, byte fillByte = 0xFF) => Enumerable.Repeat(fillByte, size);

        public static byte[] ToFixedLengthString(string str, int size, bool addEnd = true)
        {
            var buf = DefaultEncode.GetBytes(str);
            if (addEnd && buf.Length >= size)
                return buf.Take(size - 1).Append<byte>(0).ToArray();
            return buf.Concat(Enumerable.Repeat<byte>(0, size)).Take(size).ToArray();
        }

        public static string FromFixedLengthString([AllowNull] byte[] str)
        {
            if (str == null || str.Length == 0 || str[0] == '\0')
                return "";
            return DefaultEncode.GetString(str.TakeWhile(c => c != '\0').ToArray());
        }

    }
}
