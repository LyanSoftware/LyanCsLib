using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common;

public static class NumberExtensions
{
    public static bool ApproximatelyEqual(this float a, float b, float tolerance)
    => Math.Abs(a - b) < tolerance;
    public static bool ApproximatelyEqual(this double a, double b, double tolerance)
    => Math.Abs(a - b) < tolerance;
}
