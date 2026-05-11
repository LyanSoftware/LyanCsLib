using System;
using System.Runtime.InteropServices;
using SkiaSharp;

namespace Lytec.Image
{
    public static class ImageUtils
    {
        public static byte GetGrayScale(byte r, byte g, byte b) => (byte)(r * 0.299 + g * 0.587 + b * 0.114);

        public static byte GetGrayScale(this SKColor color) => GetGrayScale(color.Red, color.Green, color.Blue);

        public static float GetRedF(this SKColor color) => (color.Red + 1) / 256f;
        public static float GetGreenF(this SKColor color) => (color.Green + 1) / 256f;
        public static float GetBlueF(this SKColor color) => (color.Blue + 1) / 256f;
        public static float GetAlphaF(this SKColor color) => (color.Alpha + 1) / 256f;
        
        public static byte GetGrayScale(this Color color) => GetGrayScale(color.R, color.G, color.B);

        public static float GetRedF(this Color color) => (color.R + 1) / 256f;
        public static float GetGreenF(this Color color) => (color.G + 1) / 256f;
        public static float GetBlueF(this Color color) => (color.B + 1) / 256f;
        public static float GetAlphaF(this Color color) => (color.A + 1) / 256f;

        public static bool Save(this SKBitmap bmp, string path, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using var fs = File.OpenWrite(path);
            return bmp.Save(fs, format, quality);
        }

        public static bool Save(this SKBitmap bmp, Stream stream, SKEncodedImageFormat format = SKEncodedImageFormat.Png, int quality = 100)
        {
            using var ms = new MemoryStream();
            if (!bmp.Encode(ms, format, 100))
                return false;
            ms.Seek(0, SeekOrigin.Begin);
            ms.CopyTo(stream);
            return true;
        }

        public static SKBitmap ToSKBitmap(this ImageData img)
        {
            var width = img.Width;
            var height = img.Height;
            var bmp = new SKBitmap(new SKImageInfo(width, height, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            var rowBytes = bmp.RowBytes;   // 每行字节数（可能因内存对齐而大于 Width * 4）
            var pixels = new byte[bmp.ByteCount];
            for (int y = 0; y < height; y++)
            {
                int rowStart = y * rowBytes;
                for (int x = 0; x < width; x++)
                {
                    var offset = rowStart + x * 4; // 每像素4字节

                    var px = img[x, y].SKColor;

                    // 按 BGRA 顺序写入（注意顺序！）
                    pixels[offset] = px.Blue;
                    pixels[offset + 1] = px.Green;
                    pixels[offset + 2] = px.Red;
                    pixels[offset + 3] = px.Alpha;
                }
            }
            Marshal.Copy(pixels, 0, bmp.GetPixels(), bmp.ByteCount);
            return bmp;
        }

        public static ImageData GetImageData(this SKImage img)
        {
            var w = img.Width;
            var h = img.Height;
            // 将图像转换为非预乘RGBA8888
            using var bmp = new SKBitmap(new SKImageInfo(w, h, SKColorType.Bgra8888, SKAlphaType.Unpremul));
            using (SKCanvas canvas = new SKCanvas(bmp))
                canvas.DrawImage(img, 0, 0);
            var pixels = bmp.Bytes;
            var rowBytes = bmp.RowBytes;   // 每行字节数（可能因内存对齐而大于 Width * 4）
            return new ImageData(w, h, pos =>
            {
                var offset = pos.Y * rowBytes + pos.X * 4;
                return new Color(pixels[offset + 2], pixels[offset + 1], pixels[offset + 0], pixels[offset + 3]);
            });
        }

        public static ImageData GetImageData(this SKBitmap bmp)
        {
            using var img = SKImage.FromBitmap(bmp);
            return img.GetImageData();
        }
    }
}
