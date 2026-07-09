using System;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image;

public static class SKColorFilters
{
    public static SKColorFilter Monochrome { get; } = SKColorFilter.CreateColorMatrix(new float[]
    {
        /*         R       G       B    A  Off */
        /* R */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* G */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* B */ 0.299f, 0.587f, 0.114f, 0, 0,
        /* A */    0,      0,      0,   1, 0,
    });

    public static SKColorFilter CreateColoring(SKColor color)
    => SKColorFilter.CreateColorMatrix(new float[]
    {
        color.GetRedF(),   0, 0, 0, 0,
        0, color.GetGreenF(), 0, 0, 0,
        0, 0, color.GetBlueF(),  0, 0,
        0, 0, 0, color.GetAlphaF(), 0
    });

    public static SKColorFilter RGB111 { get; } = CreateQuantization(1, 1, 1);
    public static SKColorFilter RG11 { get; } = CreateQuantization(1, 1, 0);
    public static SKColorFilter RGB565 { get; } = CreateQuantization(5, 6, 5);

    /// <summary>
    /// 创建颜色量化滤镜，将每个通道映射到指定的位数。
    /// </summary>
    /// <param name="rBits">红色通道位数</param>
    /// <param name="gBits">绿色通道位数</param>
    /// <param name="bBits">蓝色通道位数</param>
    /// <returns>SKColorFilter 实例</returns>
    public static SKColorFilter CreateQuantization(int rBits, int gBits, int bBits)
    {
        // 每个通道需要一个 256 字节的查找表（输入值 0~255 -> 输出值 0~255）
        var rTable = BuildChannelTable(rBits);
        var gTable = BuildChannelTable(gBits);
        var bTable = BuildChannelTable(bBits);
        var aTable = new byte[256];
        for (byte i = 0; i < aTable.Length; i++)
            aTable[i] = i;
        return SKColorFilter.CreateTable(rTable, gTable, bTable, aTable);

        static byte[] BuildChannelTable(int bits)
        {
            var table = new byte[256];
            if (bits >= 8)
            {
                for (byte i = 0; i < table.Length; i++)
                    table[i] = i;
            }
            else if (bits > 0)
            {
                // 1 <= bits <= 7
                int levels = 1 << bits;                 // 离散级别数
                var maxlv = levels - 1;
                for (int i = 0; i < 256; i++)
                {
                    // 四舍五入到最近的级别
                    int level = (i * maxlv + 127) / 255;
                    // 映射回 0~255
                    int value = (level * 255 + maxlv / 2) / maxlv; ;
                    table[i] = (byte)value;
                }
            }
            return table;
        }
    }
}
