using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Data;
using SkiaSharp;

namespace Lytec.Image;

public class ImageData : IReadOnlyList<Color>
{
    public int Width { get; }
    public int Height { get; }
    public Color[] Pixels { get; }

    public int Count => Pixels.Length;
    public Color this[int index]
    {
        get => Pixels[index];
        set => Pixels[index] = value;
    }
    public IEnumerator<Color> GetEnumerator() => (IEnumerator<Color>)Pixels.GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => Pixels.GetEnumerator();

    protected virtual int GetPixelOffset(int x, int y) => x + y * Width;

    public Color this[Point point]
    {
        get => this[point.X, point.Y];
        set => this[point.X, point.Y] = value;
    }

    public Color this[int x, int y]
    {
        get => Pixels[GetPixelOffset(x, y)];
        set => Pixels[GetPixelOffset(x, y)] = value;
    }

    public ImageData(int width, int height)
    {
        Width = width;
        Height = height;
        Pixels = new Color[width * height];
    }

    public ImageData(int width, int height, Func<Point, Color> getPixel)
        : this(width, height)
    {
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
                this[x, y] = getPixel(new Point(x, y));
    }

    public ImageData(ImageData data) : this(data.Width, data.Height, pt => data[pt]) { }

    public ImageData(SKImage img) : this(img.GetImageData()) { }

    public ImageData(SKBitmap bmp) : this(bmp.GetImageData()) { }

    public static Endian SerializationEndian { get; set; } = Endian.Little;

    public byte[] Serialize()
    {
        var buf = new List<byte>(8 + Count * 4);
        buf.AddRange(Width.ToBytes(SerializationEndian));
        buf.AddRange(Height.ToBytes(SerializationEndian));
        foreach (var px in Pixels)
        {
            buf.Add(px.A);
            buf.Add(px.R);
            buf.Add(px.G);
            buf.Add(px.B);
        }
        return buf.ToArray();
    }

    public static ImageData? Deserialize(byte[] bytes, int offset = 0)
    {
        if (bytes.Length - offset < 12)
            return null;
        var w = bytes.ToStruct<int>(offset, SerializationEndian);
        offset += 4;
        var h = bytes.ToStruct<int>(offset, SerializationEndian);
        offset += 4;
        if (w < 1 || h < 1)
            return null;
        var count = w * h;
        if (bytes.Length - offset < count * 4)
            return null;
        var img = new ImageData(w, h);
        for (var i = 0; i < count; i++)
        {
            img[i] = new Color(bytes[offset + 1], bytes[offset + 2], bytes[offset + 3], bytes[offset]);
            offset += 4;
        }
        return img;
    }
}
