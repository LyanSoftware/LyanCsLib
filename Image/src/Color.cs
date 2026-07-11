using System;
using System.Collections.Generic;
using System.Text;
using Newtonsoft.Json;
using SkiaSharp;

namespace Lytec.Image;

[JsonConverter(typeof(ColorJsonConverter))]
public readonly partial struct Color : IEquatable<Color>
{
    public byte R { get; }
    public byte G { get; }
    public byte B { get; }
    public byte A { get; }

    public SKColor SKColor => new SKColor(R, G, B, A);

    public Color(byte r, byte g, byte b, byte a = 255) => (R, G, B, A) = (r, g, b, a);
    public Color(int r, int g, int b, int a = 255) => (R, G, B, A) = ((byte)r, (byte)g, (byte)b, (byte)a);
    public Color(SKColor color) : this(color.Red, color.Green, color.Blue, color.Alpha) { }
    public Color(Color color) : this(color.R, color.G, color.B, color.A) { }
    public void Deconstruct(out byte r, out byte g, out byte b, out byte a) => (r, g, b, a) = (R, G, B, A);

    public static implicit operator SKColor(Color color) => color.SKColor;
    public static implicit operator Color(SKColor color) => new(color);

    public Color(string colorName)
    {
        if (!Colors.TryGetValue(colorName, out var c))
            throw new ArgumentException("Unrecognized color name", nameof(colorName));
        R = c.R;
        G = c.G;
        B = c.B;
        A = c.A;
    }
    public static Color FromName(string colorName) => new(colorName);
    public static bool TryFromName(string colorName, out Color color) => Colors.TryGetValue(colorName, out color);

    public override bool Equals(object? obj) => obj is Color color && Equals(color);

    public bool Equals(Color other)
        => R == other.R &&
           G == other.G &&
           B == other.B &&
           A == other.A;

    public override int GetHashCode() => HashCode.Combine(R, G, B, A);

    public static bool operator ==(Color left, Color right) => left.Equals(right);
    public static bool operator !=(Color left, Color right) => !(left == right);

    public byte GrayScale => (byte)(R * 0.299 + G * 0.587 + B * 0.114);
}
