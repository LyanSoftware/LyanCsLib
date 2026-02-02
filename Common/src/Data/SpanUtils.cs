using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Data;

public static class SpanUtils
{
    public static ReadOnlySpan<T> AsReadOnlySpan<T>(this T[] arr) => arr;
    public static ReadOnlySpan<T> AsReadOnly<T>(this Span<T> span) => span;
}
