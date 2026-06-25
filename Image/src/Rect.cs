using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Image;

public readonly struct Rect : IEquatable<Rect>
{
    public int X { get; }
    public int Y { get; }
    public int Width { get; }
    public int Height { get; }

    public Point Point => new(X, Y);
    public Size Size => new(Width, Height);

    public Rect(int x, int y, int w, int h) => (X, Y, Width, Height) = (x, y, w, h);

    public override bool Equals(object? obj) => obj is Rect rect && Equals(rect);

    public bool Equals(Rect other)
    => X == other.X &&
       Y == other.Y &&
       Width == other.Width &&
       Height == other.Height;

    public override int GetHashCode() => HashCode.Combine(X, Y, Width, Height);

    public static bool operator ==(Rect left, Rect right) => left.Equals(right);

    public static bool operator !=(Rect left, Rect right) => !(left == right);
}
