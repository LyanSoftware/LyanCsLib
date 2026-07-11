using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Text;

namespace Lytec.Common;

public static class NullableExtensions
{
    public static bool IsNullable(this Type t) => t.IsNullable(out _);
    public static bool IsNullable(this Type t, [NotNullWhen(true)] out Type? UnderlyingType)
    {
        UnderlyingType = Nullable.GetUnderlyingType(t);
        return UnderlyingType != null;
    }
}
