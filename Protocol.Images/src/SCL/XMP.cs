using System;
using System.Buffers.Binary;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Lytec.Common.Data;
using Lytec.Image;
using SkiaSharp;

namespace Lytec.Protocol.Images.SCL
{
    public enum XMPColorType
    {
        Unknown = -1,
        Invalid = 0,

        FontLib,
        FontLib_SC3000,
        RG88,
        RGB565,
        RGB565SameSize,
        RG11,
        RGB111,
        RGBx8888,

        SuperComm_FullColor = RGB565,
        SuperComm_FullColorSameSize = RGB565SameSize,
        SCL2008_DualColor = RG11,
        SCL2008_FullColor = RGB111,
        SC3000_FullColor = RGBx8888,
    }

    public static class XMPUtils
    {
        public static int GetCode(this XMPColorType type) => type switch
        {
            XMPColorType.RGB565 => 0,
            XMPColorType.RGB565SameSize => 2,
            XMPColorType.RG11 => 1,
            XMPColorType.RGB111 => 3,
            XMPColorType.RGBx8888 => 8,
            _ => -1,
        };

        public static XMPColorType ToColorType(int code) => code switch
        {
            0 => XMPColorType.RGB565,
            2 => XMPColorType.RGB565SameSize,
            1 => XMPColorType.RG11,
            3 => XMPColorType.RGB111,
            8 => XMPColorType.RGBx8888,
            _ => XMPColorType.Unknown,
        };

        public static bool HasDataHeader(this XMPColorType type)
        {
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.FontLib_SC3000:
                case XMPColorType.RGB565SameSize:
                    return false;
                case XMPColorType.RG11:
                case XMPColorType.RGB111:
                case XMPColorType.RG88:
                case XMPColorType.RGB565:
                case XMPColorType.RGBx8888:
                    return true;
                default:
                    throw new ArgumentException();
            }
        }

        public static bool IsStoreByRow(this XMPColorType type)
        {
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.RGB565SameSize:
                case XMPColorType.RG11:
                case XMPColorType.RGB111:
                case XMPColorType.RG88:
                case XMPColorType.RGB565:
                    return false;
                case XMPColorType.FontLib_SC3000:
                case XMPColorType.RGBx8888:
                    return true;
                default:
                    throw new ArgumentException();
            }
        }

        public static int GetPixelBytes(this XMPColorType type)
        {
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.FontLib_SC3000:
                    return 0;
                case XMPColorType.RGB565:
                case XMPColorType.RGB565SameSize:
                    return 2;
                case XMPColorType.RG11:
                case XMPColorType.RGB111:
                    return 0;
                case XMPColorType.RG88:
                    return 2;
                case XMPColorType.RGBx8888:
                    return 4;
                default:
                    throw new ArgumentException();
            }
        }

        public static int GetBytePixels(this XMPColorType type)
        {
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.FontLib_SC3000:
                    return 8;
                case XMPColorType.RGB565:
                case XMPColorType.RGB565SameSize:
                    return 0;
                case XMPColorType.RG11:
                    return 4;
                case XMPColorType.RGB111:
                    return 2;
                case XMPColorType.RG88:
                    return 0;
                case XMPColorType.RGBx8888:
                    return 0;
                default:
                    throw new ArgumentException();
            }
        }

        public static (int Byte, int Bit) GetPixelDataOffsets(this XMPColorType type, int x, int y, int w, int h)
        {
            var storeByRow = type.IsStoreByRow();
            var pxBytes = type.GetPixelBytes();
            if (pxBytes > 0)
            {
                if (storeByRow)
                    return  ((y * w + x) * pxBytes, 0);
                else return ((x * h + y) * pxBytes, 0);
            }
            else
            {
                var bytePixels = type.GetBytePixels();
                var pxBits = 8 / bytePixels;
                if (storeByRow)
                    return (y * ((w + bytePixels - 1) / bytePixels) + x / bytePixels, x % bytePixels * pxBits);
                else return (x * ((h + bytePixels - 1) / bytePixels) + y / bytePixels, y % bytePixels * pxBits);
            }
        }

        public static int GetFrameSize(this XMPColorType type, int w, int h, bool addFrameHeader)
        => type switch
        {
            XMPColorType.FontLib => (h + 7) / 8 * w,
            XMPColorType.FontLib_SC3000 => (w + 7) / 8 * h,
            XMPColorType.RG88 => w * h * 2 + (addFrameHeader ? 4 : 0),
            XMPColorType.RGB565 => w * h * 2 + (addFrameHeader ? 4 : 0),
            XMPColorType.RGB565SameSize => w * h * 2,
            XMPColorType.RG11 => (h + 3) / 4 * w + (addFrameHeader ? 4 : 0),
            XMPColorType.RGB111 => (h + 1) / 2 * w + (addFrameHeader ? 4 : 0),
            XMPColorType.RGBx8888 => w * h * 4 + (addFrameHeader ? 4 : 0),
            _ => throw new ArgumentException(),
        };

        public static int GetFileSize(this XMPColorType type, int w, int h, bool addFrameHeader, bool addFileHeader)
        => GetFrameSize(type, w, h, addFrameHeader) + (addFileHeader ? 6 : 0);
        public static int GetFileSize(this XMPColorType type, IEnumerable<SKSizeI> sizes, bool addFrameHeader, bool addFileHeader)
        => sizes.Sum(sz => GetFrameSize(type, sz.Width, sz.Height, addFrameHeader)) + (addFileHeader ? 6 : 0);
    }

    public class XMPFile : List<ImageData>
    {
        public const Endian Endian = Lytec.Common.Data.Endian.Little;

        public int MaxWidth { get; set; }
        public int MaxHeight { get; set; }

        public void RecalcMaxSize()
        {
            MaxWidth = this.Max(g => g.Width);
            MaxHeight = this.Max(g => g.Height);
        }

        public byte[] Encode(XMPColorType type, bool addFileHeader = true)
        {
            var ro = 0;
            var go = 0;
            var bo = 0;
            var ao = 0;
            var rw = 0;
            var gw = 0;
            var bw = 0;
            var aw = 0;
            var gco = 0;
            var gcw = 0;
            var ncw = 0;
            var hasDataHeader = type.HasDataHeader();
            var storeByRow = type.IsStoreByRow();
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.FontLib_SC3000:
                    gco = 0;
                    gcw = 1;
                    break;
                case XMPColorType.RG11:
                    ro = 0; rw = 1;
                    go = 1; gw = 1;
                    break;
                case XMPColorType.RGB111:
                    ro = 0; rw = 1;
                    go = 1; gw = 1;
                    bo = 2; bw = 1;
                    ncw = 1;
                    break;
                case XMPColorType.RG88:
                    ro = 0; rw = 8;
                    go = 8; gw = 8;
                    break;
                case XMPColorType.RGB565:
                case XMPColorType.RGB565SameSize:
                    ro = 0; rw = 5;
                    go = 5; gw = 6;
                    bo = 11; bw = 5;
                    break;
                case XMPColorType.RGBx8888:
                    ro = 0; rw = 8;
                    go = 8; gw = 8;
                    bo = 16; bw = 8;
                    ao = 24; aw = 8;
                    break;
                default:
                    throw new ArgumentException();
            }
            var pxBits = rw + gw + bw + aw + gcw + ncw;
            var pxBytes = pxBits.SizeAlignTo(8) / 8;
            var bytePixels = 8 / pxBits;
            var maxw = MaxWidth;
            var maxh = MaxHeight;
            if (!hasDataHeader)
            {
                if (!this.All(g => g.Width == maxw && g.Height == maxh))
                    throw new InvalidOperationException();
            }
            else
            {
                if (!this.Take(Count - 1).All(g => g.Width == maxw && g.Height == maxh))
                    throw new InvalidOperationException();
                if (this[Count - 1].Width > this[0].Width
                    || this[Count - 1].Height > this[0].Height)
                    throw new InvalidOperationException();
            }
            var data = new List<byte[]>();
            if (addFileHeader)
            {
                // 文件头
                var buf = new List<byte>(10);
                buf.Add((byte)type.GetCode());
                buf.Add((byte)Count);
                buf.AddRange(((ushort)maxh).ToBytes(Endian));
                buf.AddRange(((ushort)maxw).ToBytes(Endian));
                switch (type)
                {
                    case XMPColorType.RG88:
                        buf[0] = 0;
                        break;
                    case XMPColorType.RGB565:
                    case XMPColorType.RGB565SameSize:
                    case XMPColorType.RG11:
                    case XMPColorType.RGB111:
                        break;
                    case XMPColorType.RGBx8888:
                        buf[1] = 0;
                        buf.AddRange(((ushort)Count).ToBytes(Endian));
                        break;
                }
                data.Add(buf.ToArray());
            }
            foreach (var img in this)
            {
                int GetPixelColor(int x, int y)
                {
                    var c = img![x, y];
                    var v = 0;
                    if (rw > 0)
                        v |= (c.R >> (8 - rw)) << ro;
                    if (gw > 0)
                        v |= (c.G >> (8 - gw)) << go;
                    if (bw > 0)
                        v |= (c.B >> (8 - bw)) << bo;
                    if (aw > 0)
                        v |= (c.A >> (8 - aw)) << ao;
                    if (gcw > 0)
                        v |= (c.GetGrayScale() >> (8 - gcw)) << gco;
                    return v;
                }
                void addimg(byte[] buf)
                {
                    if (hasDataHeader)
                    {
                        var buf2 = new byte[buf.Length + 4];
                        Array.Copy(buf, 0, buf2, 4, buf.Length);
                        buf = buf2;
                        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(0), (ushort)img!.Height);
                        BinaryPrimitives.WriteUInt16LittleEndian(buf.AsSpan(2), (ushort)img!.Width);
                    }
                    data!.Add(buf);
                }
                if (pxBits >= 8)
                {
                    var buf = new byte[pxBytes * maxw * maxh];
                    var linesize = (storeByRow ? maxw : maxh) * pxBytes;
                    for (var x = 0; x < img.Width; x++)
                    {
                        for (var y = 0; y < img.Height; y++)
                        {
                            var v = GetPixelColor(x, y);
                            if (v > 0)
                            {
                                var pxoff = storeByRow ? (y * linesize + x * pxBytes) : (x * linesize + y * pxBytes);
                                Array.Copy(v.ToBytes(Endian), 0, buf, pxoff, pxBytes);
                            }
                        }
                    }
                    addimg(buf);
                }
                else if (storeByRow)
                {
                    var linesize = (maxw + bytePixels - 1) / bytePixels;
                    var buf = new byte[linesize * maxh];
                    for (var y = 0; y < img.Height; y++)
                    {
                        var lineoff = y * linesize;
                        for (var x = 0; x < img.Width;)
                        {
                            var byteoff = lineoff + x;
                            for (var pi = 0; pi < bytePixels && x < img.Width; pi++, x++)
                            {
                                var v = GetPixelColor(x, y);
                                if (v > 0)
                                {
                                    var bitoff = pi * pxBits;
                                    buf[byteoff] |= (byte)(v & BitHelper.MakeMask(pxBits));
                                }
                            }
                        }
                    }
                    addimg(buf);
                }
                else
                {
                    var linesize = (maxh + bytePixels - 1) / bytePixels;
                    var buf = new byte[linesize * maxw];
                    for (var x = 0; x < img.Width; x++)
                    {
                        var lineoff = x * linesize;
                        for (var y = 0; y < img.Height;)
                        {
                            var byteoff = lineoff + y;
                            for (var pi = 0; pi < bytePixels && y < img.Height; pi++, y++)
                            {
                                var v = GetPixelColor(x, y);
                                if (v > 0)
                                {
                                    var bitoff = pi * pxBits;
                                    buf[byteoff] |= (byte)(v & BitHelper.MakeMask(pxBits));
                                }
                            }
                        }
                    }
                    addimg(buf);
                }
            }
            {
                var buf = new byte[data.Sum(v => v.Length)];
                for (int i = 0, off = 0; i < data.Count; i++)
                {
                    Array.Copy(data[i], 0, buf, off, data[i].Length);
                    off += data[i].Length;
                }
                return buf;
            }
        }

        public record DecodeInfo(XMPColorType ColorType);

        public static bool Decode(byte[] data, [NotNullWhen(true)] out XMPFile? xmp)
        => Decode(data, out xmp, out _);
        public static bool Decode(byte[] data, [NotNullWhen(true)] out XMPFile? xmp, [NotNullWhen(true)] out DecodeInfo? Info)
        {
            xmp = null;
            if (data.Length < 7)
                throw new IndexOutOfRangeException();

            var offset = 0;
            var type = XMPUtils.ToColorType(data[offset++]);
            if (type == XMPColorType.Invalid || type == XMPColorType.Unknown)
                throw new InvalidDataException();
            int count = data[offset++];
            var maxh = data.ToStruct<ushort>(offset, Endian);
            offset += 2;
            var maxw = data.ToStruct<ushort>(offset, Endian);
            offset += 2;
            if (type == XMPColorType.RGBx8888)
            {
                if (data.Length < (offset + 3))
                    throw new IndexOutOfRangeException();
                count = data.ToStruct<ushort>(offset, Endian);
                offset += 2;
            }

            var ro = 0;
            var go = 0;
            var bo = 0;
            var ao = 0;
            var rw = 0;
            var gw = 0;
            var bw = 0;
            var aw = 0;
            var gco = 0;
            var gcw = 0;
            var ncw = 0;
            var hasDataHeader = type.HasDataHeader();
            var storeByRow = type.IsStoreByRow();
            switch (type)
            {
                case XMPColorType.FontLib:
                case XMPColorType.FontLib_SC3000:
                    gco = 0;
                    gcw = 1;
                    break;
                case XMPColorType.RG11:
                    ro = 0; rw = 1;
                    go = 1; gw = 1;
                    break;
                case XMPColorType.RGB111:
                    ro = 0; rw = 1;
                    go = 1; gw = 1;
                    bo = 2; bw = 1;
                    ncw = 1;
                    break;
                case XMPColorType.RG88:
                    ro = 0; rw = 8;
                    go = 8; gw = 8;
                    break;
                case XMPColorType.RGB565:
                case XMPColorType.RGB565SameSize:
                    ro = 0; rw = 5;
                    go = 5; gw = 6;
                    bo = 11; bw = 5;
                    break;
                case XMPColorType.RGBx8888:
                    ro = 0; rw = 8;
                    go = 8; gw = 8;
                    bo = 16; bw = 8;
                    ao = 24; aw = 8;
                    break;
                default:
                    throw new ArgumentException();
            }
            var pxBits = rw + gw + bw + aw + gcw + ncw;
            var pxBytes = pxBits.SizeAlignTo(8) / 8;
            var bytePixels = 8 / pxBits;
            int GetImgSize(int w, int h)
            {
                if (pxBytes > 0)
                    return w * h * pxBytes;
                else if (storeByRow)
                    return (w + bytePixels - 1) / bytePixels * h;
                else return (h + bytePixels - 1) / bytePixels * w;
            }

            xmp = new XMPFile();
            for (var i = 0; i < count; i++)
            {
                var w = maxw;
                var h = maxh;
                if (hasDataHeader)
                {
                    if (data.Length < (offset + 5))
                        throw new IndexOutOfRangeException();
                    h = data.ToStruct<ushort>(offset, Endian);
                    offset += 2;
                    w = data.ToStruct<ushort>(offset, Endian);
                    offset += 2;
                    if (w > maxw || h > maxh)
                        throw new InvalidDataException();
                }
                var sz = GetImgSize(w, h);
                if (data.Length < (offset + sz))
                    throw new IndexOutOfRangeException();
                var img = new ImageData(w, h);
                Color ConvColor(int v)
                {
                    var r = 0;
                    var g = 0;
                    var b = 0;
                    var a = 0xff;
                    if (rw > 0)
                        r = (v >> ro) & (int)BitHelper.MakeMask(rw);
                    if (gw > 0)
                        g = (v >> go) & (int)BitHelper.MakeMask(gw);
                    if (bw > 0)
                        b = (v >> bo) & (int)BitHelper.MakeMask(bw);
                    if (aw > 0)
                        a = (v >> ao) & (int)BitHelper.MakeMask(aw);
                    if (gcw > 0 && (rw + gw + bw + aw == 0))
                        r = g = b = (v >> gco) & (int)BitHelper.MakeMask(gcw);
                    return new((byte)r, (byte)g, (byte)b, (byte)a);
                }
                if (pxBits >= 8)
                {
                    for (var x = 0; x < w; x++)
                    {
                        for (var y = 0; y < h; y++)
                        {
                            var v = 0;
                            for (var di = 0; di < pxBytes; di++)
                                v |= data[offset + di] << (di * 8);
                            img[x, y] = ConvColor(v);
                        }
                    }
                }
                else if (storeByRow)
                {
                    var linesize = (maxw + bytePixels - 1) / bytePixels;
                    for (var y = 0; y < h; y++)
                    {
                        var lineoff = y * linesize;
                        for (var x = 0; x < w;)
                        {
                            var byteoff = lineoff + x;
                            for (var pi = 0; pi < bytePixels && x < w; pi++, x++)
                            {
                                var bitoff = pi * pxBits;
                                var v = (data[offset + byteoff] >> bitoff) & (int)BitHelper.MakeMask(pxBits);
                                img[x, y] = ConvColor(v);
                            }
                        }
                    }
                }
                else
                {
                    var linesize = (maxh + bytePixels - 1) / bytePixels;
                    for (var x = 0; x < w; x++)
                    {
                        var lineoff = x * linesize;
                        for (var y = 0; y < h;)
                        {
                            var byteoff = lineoff + y;
                            for (var pi = 0; pi < bytePixels && y < h; pi++, y++)
                            {
                                var bitoff = pi * pxBits;
                                var v = (data[offset + byteoff] >> bitoff) & (int)BitHelper.MakeMask(pxBits);
                                img[x, y] = ConvColor(v);
                            }
                        }
                    }
                }
                offset += sz;
                xmp.Add(img);
            }

            Info = new DecodeInfo(type);
            return true;
        }

        public static bool TryDecode(byte[] data, [NotNullWhen(true)] out XMPFile? xmp)
        => TryDecode(data, out xmp, out _);
        public static bool TryDecode(byte[] data, [NotNullWhen(true)] out XMPFile? xmp, [NotNullWhen(true)] out DecodeInfo? Info)
        {
            try
            {
                return Decode(data, out xmp, out Info);
            }
            catch (Exception)
            {
                xmp = null;
                Info = null;
                return false;
            }
        }
    }
}
