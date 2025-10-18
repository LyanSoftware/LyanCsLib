using System;
using SkiaSharp;

namespace Lytec.Image
{
    public static class ImageUtils
    {
        public static byte GetGrayScale(this SKColor color) => (byte)(color.Red * 0.299 + color.Green * 0.587 + color.Blue * 0.114);

        public static float GetRedF(this SKColor color) => (color.Red + 1) / 256f;
        public static float GetGreenF(this SKColor color) => (color.Green + 1) / 256f;
        public static float GetBlueF(this SKColor color) => (color.Blue + 1) / 256f;
        public static float GetAlphaF(this SKColor color) => (color.Alpha + 1) / 256f;

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
    }
}
