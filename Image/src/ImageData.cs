using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using SkiaSharp;

namespace Lytec.Image
{
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

        protected virtual int GetPixelOffset(int x, int y) => x + y * Height;

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
            for (var x = 0; x < width; x++)
                for (var y = 0; y < height; y++)
                    this[x, y] = getPixel(new Point(x, y));
        }

        public ImageData(ImageData data) : this(data.Width, data.Height, pt => data[pt]) { }

        public ImageData(SKImage img) : this(img.GetImageData()) { }

        public ImageData(SKBitmap bmp) : this(bmp.GetImageData()) { }
    }

}
