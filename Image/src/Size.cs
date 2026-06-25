using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Image;

public readonly struct Size : IEquatable<Size>
{
    public int Width { get; }
    public int Height { get; }

    public Size(int w, int h) => (Width, Height) = (w, h);

    public override bool Equals(object? obj) => obj is Size size && Equals(size);
    public bool Equals(Size other) => Width == other.Width && Height == other.Height;
    public override int GetHashCode() => HashCode.Combine(Width, Height);
    public static bool operator ==(Size left, Size right) => left.Equals(right);
    public static bool operator !=(Size left, Size right) => !(left == right);
}
