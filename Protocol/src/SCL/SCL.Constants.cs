using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Data;

namespace Lytec.Protocol;

partial class SCL
{
    public static class Constants
    {
        public const Endian DefaultEndian = Endian.Little;

        public const string NewLine = "\r\n";
        public const string PathSeparator = "\\";
        public const int NameSize = 16;
        public const int MaxDirNameLength = 3; // 三字节子目录
        public const int MaxFilePathLength = MaxDirNameLength + 1 + 12; // 子目录 + '\' + 8.3格式文件名
        public const int GBufferSize = 2 * 1024 * 1024;
        public const int MaxFileSize = GBufferSize;
        public const int FalseValue = -1;
        public const int MaxDataLength = 1024;
        public const int MaxFontLibCount = 8; // 最大字库数量
        public const string ConfigFileName = "CONFIG.LY";
        public const string OtherConfigFileName = "SYSTEM.LY";
        public const string PlaylistFileName = "PLAYLIST.LY";

        public const string SuPw = "LyTeClYtEc";

        public const int FlashWriteMaxFileSeconds = 50; // Flash写入最大文件所需时间
        public const int FlashReadMaxFileSeconds = 24; // Flash读取最大文件所需时间（暂停播放时为16秒）
        public const int FlashWriteBytesPerSecond = GBufferSize / FlashWriteMaxFileSeconds; // Flash写入速度
        public const int FlashReadBytesPerSecond = GBufferSize / FlashReadMaxFileSeconds; // Flash读取速度

    }
}
