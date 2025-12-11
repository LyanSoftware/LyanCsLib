using System.Diagnostics;
using Lytec.Common.Data;
using Org.BouncyCastle.Ocsp;

namespace Lytec.Protocol;

[Serializable]
[DebuggerDisplay("{" + nameof(DebugView) + "}")]
public struct Rgba8888Color
{
    public uint Value
    {
        get => (uint)(R | (G << 8) | (B << 16) | (A << 24));
        set => (R, G, B, A) = ((byte)(value & 0xFF), (byte)((value >> 8) & 0xff), (byte)((value >> 16) & 0xff), (byte)(value >> 24));
    }

    public string DebugView => $"#{A:X2}{R:X2}{G:X2}{B:X2}";

    public byte R { get; set; }
    public byte G { get; set; }
    public byte B { get; set; }
    public byte A { get; set; }

    public Rgba8888Color(uint color) => Value = color;
    public Rgba8888Color(int r, int g, int b, int a = 0xff) => (R, G, B, A) = ((byte)r, (byte)g, (byte)b, (byte)a);

    public byte GrayScale => (byte)(R * 0.299 + G * 0.587 + B * 0.114);
    public byte ToRGB111() => (byte)((R / 128) | ((G / 128) << 1) | ((B / 128) << 2));
}
