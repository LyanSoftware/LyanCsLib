using System.ComponentModel;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public enum MediaType
{
    [Description("文本")]
    Text = 1,
    [Description("图片")]
    Image = 2,
    [Description("视频")]
    Video = 3,
    [Description("Gif")]
    Gif = 4,
}
