using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Image;

public readonly struct Point
{
    public int X { get; }
    public int Y { get; }

    public Point(int x, int y) => (X, Y) = (x, y);
}
