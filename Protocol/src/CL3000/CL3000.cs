using System.Runtime.InteropServices;

namespace Lytec.Protocol.CL3000;
public enum ColorType
{
    Monochrome = 0,
    DoubleColor = 1,
    FullColor = 2,
}

public delegate (bool R, bool G, bool B) GetPixelOneBitColor(int x, int y);

public delegate (int R, int G, int B) GetPixelColor(int x, int y);

public class Lib
{
    public static byte[] Bmp2Xmpx(GetPixelColor getPixel, int width, int height, ColorType colorType, int threshold = 128) => Bmp2Xmpx((x, y) =>
    {
        var (R, G, B) = getPixel(x, y);
        return (R >= threshold, G >= threshold, B >= threshold);
    }, width, height, colorType);
    public static byte[] Bmp2Xmpx(GetPixelOneBitColor getPixel, int width, int height, ColorType colorType)
    {
        var colSize = (height + 7) / 8;
        var colorCount = (int)colorType + 1;
        var colorSize = colSize * width;
        var xmpx = new byte[colorSize * colorCount];
        for (var x = 0; x < width; x++)
        {
            var colOffset = x * colSize;
            for (var y = 0; y < height; y++)
            {
                var (R, G, B) = getPixel(x, y);
                var byteOffset = colOffset + y / 8;
                var bitData = (byte)(1 << (y % 8));
                if (R)
                    xmpx[byteOffset] |= bitData;
                if (colorCount > 1)
                {
                    byteOffset += colorSize;
                    if (G)
                        xmpx[byteOffset] |= bitData;
                    if (colorCount > 2)
                    {
                        byteOffset += colorSize;
                        if (B)
                            xmpx[byteOffset] |= bitData;
                    }
                }
            }
        }
        return xmpx;
    }

}
