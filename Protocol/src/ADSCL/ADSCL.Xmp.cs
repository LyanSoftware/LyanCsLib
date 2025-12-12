using System;
using System.Collections.Generic;
using System.Text;
using Lytec.Common.Communication;
using Lytec.Common.Data;

namespace Lytec.Protocol
{
    partial class ADSCL
    {
        public enum XmpType
        {
            R1 = -1,
            RG11 = 1,
            RGBn1111 = 3,
            RGB565 = 0,
            RG88 = -2,
            RGB565_SameSize = 2,
            RGBA8888 = 8,
        }

        public class XmpFile : IPackage
        {
            public IReadOnlyList<Xmp> Images { get; }
            public XmpFile(IReadOnlyList<Xmp> imgs)
            {
                if (imgs.Count == 0)
                    throw new ArgumentException();
                if (!imgs.All(g => g.Type == imgs[0].Type))
                    throw new ArgumentException();
                int maxCount = byte.MaxValue;
                if (imgs[0].Type == XmpType.RGBA8888)
                    maxCount = ushort.MaxValue;
                if (imgs.Count > maxCount)
                    throw new ArgumentException();
                Images = imgs;
            }
            public byte[] Serialize()
            {
                byte[] head;
                switch (Images[0].Type)
                {
                    case XmpType.R1:
                        head = Array.Empty<byte>();
                        break;
                    case XmpType.RG11:
                    case XmpType.RGBn1111:
                    case XmpType.RGB565:
                    case XmpType.RG88:
                    case XmpType.RGB565_SameSize:
                        head = new byte[6];
                        head[0] = (byte)Images[0].Type;
                        if (Images[0].Type == XmpType.RG88)
                            head[0] = (byte)XmpType.RGB565;
                        head[1] = (byte)Images.Count;
                        Array.Copy(((ushort)Images.Max(g => g.Height)).ToBytes(Endian.Little), 0, head, 2, 2);
                        Array.Copy(((ushort)Images.Max(g => g.Width)).ToBytes(Endian.Little), 0, head, 4, 2);
                        break;
                    case XmpType.RGBA8888:
                        head = new byte[8];
                        head[0] = (byte)Images[0].Type;
                        head[1] = 0;
                        Array.Copy(((ushort)Images.Max(g => g.Height)).ToBytes(Endian.Little), 0, head, 2, 2);
                        Array.Copy(((ushort)Images.Max(g => g.Width)).ToBytes(Endian.Little), 0, head, 4, 2);
                        Array.Copy(((ushort)Images.Count).ToBytes(Endian.Little), 0, head, 6, 2);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                var buf = new byte[head.Length + Images.Sum(g => g.GetDataSize(true))];
                Array.Copy(head, buf, head.Length);
                var offset = head.Length;
                for (var i = 0; i < Images.Count; i++)
                {
                    var imgbuf = Images[i].Serialize(true);
                    Array.Copy(imgbuf, 0, buf, offset, imgbuf.Length);
                    offset += imgbuf.Length;
                }
                return buf;
            }
            public static XmpFile? Deserialize(byte[] bytes, int offset = 0)
            {
                if (bytes.Length - offset < 10)
                    return null;
                try
                {
                    var type = (XmpType)bytes[offset++];
                    switch (type)
                    {
                        case XmpType.R1:
                        //case XmpType.RG88: // RGB565与RG88为相同type, 无法识别
                            return null;
                        case XmpType.RG11:
                        case XmpType.RGBn1111:
                        case XmpType.RGB565:
                        case XmpType.RGB565_SameSize:
                        case XmpType.RGBA8888:
                            {
                                if (bytes.Length - offset < 8)
                                    return null;
                                if (type == XmpType.RGBA8888)
                                {
                                    if (bytes[offset++] != 0)
                                        return null;
                                }
                                var hasExtHead = type != XmpType.RGB565_SameSize;
                                var count = 0;
                                if (type != XmpType.RGBA8888)
                                    count = bytes[offset++];
                                var h = bytes.ToStruct<ushort>(offset, Endian.Little);
                                offset += 2;
                                var w = bytes.ToStruct<ushort>(offset, Endian.Little);
                                offset += 2;
                                if (type == XmpType.RGBA8888)
                                {
                                    count = bytes.ToStruct<ushort>(offset, Endian.Little);
                                    offset += 2;
                                }
                                var imgs = new List<Xmp>();
                                if (hasExtHead)
                                {
                                    for (var i = 0; i < count; i++)
                                    {
                                        if (bytes.Length - offset < 5)
                                            return null;
                                        var xh = bytes.ToStruct<ushort>(offset, Endian.Little);
                                        offset += 2;
                                        var xw = bytes.ToStruct<ushort>(offset, Endian.Little);
                                        offset += 2;
                                        var sz = Xmp.GetDataSize(type, xw, xh);
                                        if (bytes.Length - offset < sz)
                                            return null;
                                        imgs.Add(Xmp.Parse(type, xw, xh, bytes, offset));
                                        offset += sz;
                                    }
                                }
                                else
                                {
                                    var sz = Xmp.GetDataSize(type, w, h);
                                    if (bytes.Length - offset < count * sz)
                                        return null;
                                    for (var i = 0; i < count; i++)
                                    {
                                        imgs.Add(Xmp.Parse(type, w, h, bytes, offset));
                                        offset += sz;
                                    }
                                }
                                return new(imgs);
                            }
                        default:
                            return null;
                    }
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public class Xmp : IPackage
        {
            public XmpType Type { get; set; }
            public ushort Width { get; protected set; }
            public ushort Height { get; protected set; }
            public Rgba8888Color[] Pixels { get; protected set; } = Array.Empty<Rgba8888Color>();

            public Rgba8888Color this[int x, int y]
            {
                get => Pixels[y * Width + x];
                set => Pixels[y * Width + x] = value;
            }

            public Xmp() { }
            public Xmp(XmpType type, ushort width, ushort height)
            {
                Type = type;
                Width = width;
                Height = height;
                Pixels = new Rgba8888Color[width * height];
            }
            public delegate Rgba8888Color GetPixelColor(int x, int y);
            public Xmp(XmpType type, ushort width, ushort height, GetPixelColor getPixelColor)
                : this(type, width, height)
            {
                for (var y = 0; y < height; y++)
                    for (var x = 0; x < width; x++)
                        this[x, y] = getPixelColor(x, y);
            }

            public static int GetDataSize(XmpType type, int width, int height, bool addHeader = false) => type switch
            {
                XmpType.R1 => (height + 7) / 8 * width,
                XmpType.RG11 => (height + 3) / 4 * width + (addHeader ? 4 : 0),
                XmpType.RGBn1111 => (height + 1) / 2 * width + (addHeader ? 4 : 0),
                XmpType.RGB565 or XmpType.RG88 => width * height * 2 + (addHeader ? 4 : 0),
                XmpType.RGB565_SameSize => width * height * 2,
                XmpType.RGBA8888 => width * height * 4 + (addHeader ? 4 : 0),
                _ => throw new NotSupportedException(),
            };
            public int GetDataSize(XmpType type, bool addHeader = false) => GetDataSize(type, Width, Height, addHeader);
            public int GetDataSize(bool addHeader = false) => GetDataSize(Type, addHeader);

            public byte[] Serialize(XmpType type, byte threshold = 127, bool addHeader = false)
            {
                var w = Width;
                var h = Height;
                var head = Array.Empty<byte>();
                if (addHeader)
                {
                    switch (type)
                    {
                        case XmpType.R1:
                        case XmpType.RGB565_SameSize:
                            break;
                        case XmpType.RG11:
                        case XmpType.RGBn1111:
                        case XmpType.RGB565:
                        case XmpType.RG88:
                        case XmpType.RGBA8888:
                            head = new byte[4];
                            Array.Copy(((ushort)h).ToBytes(Endian.Little), 0, head, 0, 2);
                            Array.Copy(((ushort)w).ToBytes(Endian.Little), 0, head, 2, 2);
                            break;
                        default:
                            throw new NotSupportedException();
                    }
                }
                var buf = new byte[head.Length + GetDataSize(type, addHeader)];
                Array.Copy(head, buf, head.Length);
                var offset = head.Length;
                Action<int, int, Rgba8888Color> setPixel;
                switch (type)
                {
                    case XmpType.R1:
                        {
                            var fh = (h + 7) / 8;
                            setPixel = (x, y, c) => buf[offset + x * fh + y / 8] |= (byte)((c.GrayScale > threshold ? 1 : 0) << (y % 8));
                        }
                        break;
                    case XmpType.RG11:
                        {
                            var fh = (h + 3) / 4;
                            setPixel = (x, y, c) => buf[offset + x * fh + y / 4] |= (byte)(((c.R > threshold ? 1 : 0) | ((c.G > threshold ? 1 : 0) << 1)) << (y % 4 * 2));
                        }
                        break;
                    case XmpType.RGBn1111:
                        {
                            var fh = (h + 1) / 2;
                            setPixel = (x, y, c) => buf[offset + x * fh + y / 2] |= (byte)(((c.R > threshold ? 1 : 0) | ((c.G > threshold ? 1 : 0) << 1) | ((c.B > threshold ? 1 : 0) << 2)) << (y % 2 * 4));
                        }
                        break;
                    case XmpType.RGB565:
                    case XmpType.RGB565_SameSize:
                        setPixel = (x, y, c) => Array.Copy(new RGB565Color(c).Data.ToBytes(Endian.Little), 0, buf, offset + (x * h + y) * 2, 2);
                        break;
                    case XmpType.RG88:
                        setPixel = (x, y, c) => Array.Copy(new byte[] { c.R, c.G }, 0, buf, offset + (x * h + y) * 2, 2);
                        break;
                    case XmpType.RGBA8888:
                        setPixel = (x, y, c) => Array.Copy(c.Value.ToBytes(Endian.Little), 0, buf, offset + (y * w + x) * 2, 4);
                        break;
                    default:
                        throw new NotSupportedException();
                }
                for (var x = 0; x < w; x++)
                    for (var y = 0; y < h; y++)
                        setPixel(x, y, this[x, y]);
                return buf;
            }
            public byte[] Serialize(bool addHeader) => Serialize(Type, 127, addHeader);
            public byte[] Serialize() => Serialize(Type, 127, false);

            public static Xmp Parse(XmpType type, ushort width, ushort height, byte[] data, int offset = 0)
            {
                var datasize = GetDataSize(type, width, height, false);
                if (datasize > data.Length - offset)
                    throw new IndexOutOfRangeException();

                GetPixelColor getPixel;
                switch (type)
                {
                    case XmpType.R1:
                        {
                            var fh = (height + 7) / 8;
                            getPixel = (x, y) => new(((data[offset + x * fh + y / 8] >> (y % 8)) & 1) == 0 ? 0 : 0xffffffff);
                        }
                        break;
                    case XmpType.RG11:
                        {
                            var fh = (height + 3) / 4;
                            getPixel = (x, y) =>
                            {
                                var c = BitHelper.GetValue(data[offset + x * fh + y / 4] >> (y % 4 * 2), 0, 2);
                                return new(BitHelper.GetFlag(c, 0) ? 255 : 0, BitHelper.GetFlag(c, 1) ? 255 : 0, 0);
                            };
                        }
                        break;
                    case XmpType.RGBn1111:
                        {
                            var fh = (height + 2) / 1;
                            getPixel = (x, y) =>
                            {
                                var c = BitHelper.GetValue(data[offset + x * fh + y / 2] >> (y % 2 * 4), 0, 3);
                                return new(BitHelper.GetFlag(c, 0) ? 255 : 0, BitHelper.GetFlag(c, 1) ? 255 : 0, BitHelper.GetFlag(c, 2) ? 255 : 0);
                            };
                        }
                        break;
                    case XmpType.RGB565:
                    case XmpType.RGB565_SameSize:
                        getPixel = (x, y) => new RGB565Color(data.ToStruct<ushort>(offset + (x * height + y) * 2, Endian.Little)).ToRgba8888();
                        break;
                    case XmpType.RG88:
                        getPixel = (x, y) =>
                        {
                            var pos = offset + x * height * 2 + y;
                            return new Rgba8888Color(data[pos], data[pos + 1], 0);
                        };
                        break;
                    case XmpType.RGBA8888:
                        getPixel = (x, y) => new(data.ToStruct<uint>(offset + (y * width + x) * 4, Endian.Little));
                        break;
                    default:
                        throw new NotSupportedException();
                }
                return new Xmp(type, width, height, getPixel);
            }
            public static Xmp Parse(XmpType type, byte[] data, int offset = 0)
            {
                if (data.Length - offset < 5)
                    throw new IndexOutOfRangeException();
                var h = data.ToStruct<ushort>(offset, Endian.Little);
                offset += 2;
                var w = data.ToStruct<ushort>(offset, Endian.Little);
                offset += 2;
                return Parse(type, w, h, data, offset);
            }
        }
    }
}
