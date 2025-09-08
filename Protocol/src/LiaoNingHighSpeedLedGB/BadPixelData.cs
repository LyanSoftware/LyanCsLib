using System.Text;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public class BadPixelData
{
    public static string Encode(int width, int height, IEnumerable<Point> badPixels)
    {
        int rowSize = (width + 3) / 4;
        byte[] data = new byte[rowSize * height];
        if (badPixels != null)
        {
            var pxs = badPixels.ToList();
            for (int i = 0; i < pxs.Count; i++)
            {
                Point px = pxs[i];
                data[px.Y * rowSize + px.X / 4] |= (byte)(1 << (px.X % 4));
            }
        }
        StringBuilder sb = new StringBuilder();
        foreach (var b in data)
            sb.Append((char)(b < 10 ? (b + '0') : (b - 10 + 'A')));
        return sb.ToString();
    }

    public static Point[] Decode(int width, int height, string data)
    {
        int rowSize = (width + 3) / 4;
        char[] chrs = data.ToCharArray();
        byte[] buf = new byte[chrs.Length];
        for (int i = 0; i < chrs.Length; i++)
        {
            char ch = chrs[i];
            if (ch >= '0' && ch <= '9')
                buf[i] = (byte)(ch - '0');
            else if (ch >= 'a' && ch <= 'f')
                buf[i] = (byte)(ch - 'a' + 10);
            else if (ch >= 'A' && ch <= 'F')
                buf[i] = (byte)(ch - 'A' + 10);
        }
        var pts = new List<Point>();
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                if (((buf[y * rowSize + x / 4] >> (x % 4)) & 1) == 1)
                    pts.Add(new(x, y));
            }
        }
        return pts.ToArray();
    }
}

[Flags]
public enum BadColor
{
    None = 0,
    Red = 1 << 0,
    Green = 1 << 1,
    Blue = 1 << 2,
    Yellow = 1 << 3, // 琥珀色
}

public record Point(int X, int Y);

public record BadPixel(int X, int Y, BadColor Colors) : Point(X, Y);

public record PixelErrorData(int badCount, Size resolution, BadColor TotalColors, IReadOnlyList<BadPixel> badPixels, string Time = "");
