using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Data;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using static Lytec.Protocol.SCL.Constants;
using Lytec.Common.Communication;

namespace Lytec.Protocol
{
    partial class SCL
    {
        [Serializable]
        [Endian(DefaultEndian)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Point : IPackage
        {
            private short _x, _y;
            public int X { get => _x; set => _x = (short)value; }
            public int Y { get => _y; set => _y = (short)value; }

            public Point(int x, int y) => (_x, _y) = ((short)x, (short)y);

            public override string ToString() => $"({X}, {Y})";

            public byte[] Serialize() => this.ToBytes();
        }

        public static class Colors
        {
            public static Color Black { get; } = new Color(0, 0, 0);
            public static Color White { get; } = new Color(255, 255, 255);
            public static Color Red { get; } = new Color(255, 0, 0);
            public static Color Green { get; } = new Color(0, 255, 0);
            public static Color Blue { get; } = new Color(0, 0, 255);
            public static Color Yellow { get; } = new Color(255, 255, 0);
            public static Color Pink { get; } = new Color(0, 255, 255);
        }

        public struct Color
        {
            public int Alpha { get; set; }
            public int Red { get; set; }
            public int Green { get; set; }
            public int Blue { get; set; }

            public Color(int alpha, int red, int green, int blue) => (Alpha, Red, Green, Blue) = (alpha, red, green, blue);
            public Color(int red, int green, int blue) : this(255, red, green, blue) { }

            public byte GetGrayScale() => (byte)(Red * 0.299 + Green * 0.587 + Blue * 0.114);
            public byte To1BitRGBColor() => (byte)((Red / 128) | ((Green / 128) << 1) | ((Blue / 128) << 2));
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ColorType
        {
            FontLib,
            DualColor,
            FullColor,
            FullColorWithGreyscale,
            FullColorWithGreyscaleSameSize
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum XMPType : byte
        {
            FontLib = unchecked((byte)-1),
            FullColor16Bit = 0,
            DualColor = 1,
            FullColor16BitSameSize = 2,
            FullColor = 3,
            FullColor32Bit = 4
        }

        [Serializable]
        [Endian(DefaultEndian)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct XMPSize : IPackage
        {
            public ushort Height { get; set; }
            public ushort Width { get; set; }

            public XMPSize(ushort width, ushort height) => (Width, Height) = (width, height);

            public byte[] Serialize() => this.ToBytes();
            public static XMPSize Deserialize(byte[] buf, int offset = 0) => buf.ToStruct<XMPSize>(offset);
        }

        [Serializable]
        [Endian(DefaultEndian)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct XMPHeader : IPackage
        {
            public XMPType Type { get; set; }
            public int ImageCount { get => _ImageCount; set => _ImageCount = (byte)value; }
            private byte _ImageCount;
            public XMPSize LargestImageSize { get; set; }

            public XMPHeader(XMPType type, int imgCount, XMPSize largestImgSize) : this()
            => (Type, ImageCount, LargestImageSize) = (type, imgCount, largestImgSize);

            public byte[] Serialize() => this.ToBytes();
            public static XMPHeader Deserialize(byte[] buf, int offset = 0) => buf.ToStruct<XMPHeader>(offset);
        }

        public readonly struct ImageInfo
        {
            public Func<Point, Color> GetPixel { get; }
            public ushort Width { get; }
            public ushort Height { get; }

            public ImageInfo(Func<Point, Color> getPixel, ushort width, ushort height) => (GetPixel, Width, Height) = (getPixel, width, height);
        }

        public static XMPType ColorTypeToXMPType(ColorType type)
        {
            switch (type)
            {
                case ColorType.FontLib:
                    return XMPType.FontLib;
                case ColorType.FullColorWithGreyscaleSameSize:
                    return XMPType.FullColor16BitSameSize;
                case ColorType.DualColor:
                    return XMPType.DualColor;
                case ColorType.FullColor:
                    return XMPType.FullColor;
                case ColorType.FullColorWithGreyscale:
                    return XMPType.FullColor16Bit;
                default:
                    throw new NotImplementedException();
            }
        }

        public static byte[] GetXMPBitmapHeader(ushort width, ushort height, ColorType type)
        {
            switch (type)
            {
                case ColorType.FontLib:
                case ColorType.FullColorWithGreyscaleSameSize:
                    return new byte[0];
                case ColorType.DualColor:
                case ColorType.FullColor:
                case ColorType.FullColorWithGreyscale:
                    return new XMPSize(width, height).Serialize();
                default:
                    throw new NotImplementedException();
            }
        }

        public static byte[] GetXMPHeader(ushort width, ushort height, int imgCount, ColorType type)
        => new XMPHeader(ColorTypeToXMPType(type), imgCount, new XMPSize(width, height)).Serialize();

        public static byte[] ConvertToXMP(ImageInfo info, ColorType xmpType, bool addHeader = true)
        => ConvertToXMP(info.GetPixel, info.Width, info.Height, xmpType, addHeader);

        public static byte[] ConvertToXMP(Func<Point, Color> GetPixel, ushort width, ushort height, ColorType type, bool addHeader = true)
        {
            int pow;
            switch (type)
            {
                case ColorType.FontLib: pow = 1; break;
                case ColorType.DualColor: pow = 2; break;
                case ColorType.FullColor: pow = 3; break;
                case ColorType.FullColorWithGreyscale:
                case ColorType.FullColorWithGreyscaleSameSize:
                    throw new NotImplementedException();
                default: throw new ArgumentException("Invalid SCL color type", nameof(type));
            }
            var pixelBits = 1 << (pow - 1);
            var pixelPerBytes = 8 / pixelBits;
            var bitmapHeader = GetXMPBitmapHeader(width, height, type);
            var bytes = new byte[(height + pixelPerBytes - 1) / pixelPerBytes * width + bitmapHeader.Length];
            Array.Copy(bitmapHeader, bytes, bitmapHeader.Length);
            for (int x = 0, pxOffset = bitmapHeader.Length; x < width; x++)
            {
                for (var y = 0; y < height;)
                {
                    int b = 0;
                    for (var i = 0; i < pixelPerBytes && y < height; i++, y++)
                    {
                        var c = GetPixel(new Point(x, y));
                        if (pow != 1)
                        {
                            var px = 0;
                            if (c.Red >= 0x80)
                                px |= 1;
                            if (pixelBits > 1 && c.Green >= 0x80)
                                px |= 1 << 1;
                            if (pixelBits > 2 && c.Blue >= 0x80)
                                px |= 1 << 2;
                            if (pixelBits > 3 && c.Alpha >= 0x80)
                                px |= 1 << 3;
                            b |= px << (i * pixelBits);
                        }
                        else b |= (c.GetGrayScale() > 127 ? 1 : 0) << (i * pixelBits);
                    }
                    bytes[pxOffset++] = (byte)b;
                }
            }
            return addHeader ? GetXMPHeader(width, height, 1, type).Concat(bytes).ToArray() : bytes;
        }

        public static byte[] ConvertToXMP(IEnumerable<ImageInfo> infos, ColorType xmpType)
        {
            var data = new List<byte>();
            foreach (var info in infos)
            {
                var bytes = ConvertToXMP(info, xmpType, false);
                if (data.Count < 1)
                    data.AddRange(GetXMPHeader(info.Width, info.Height, infos.Count(), xmpType));
                data.AddRange(bytes);
            }
            return data.ToArray();
        }
    }
}
