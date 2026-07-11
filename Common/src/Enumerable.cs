using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;

namespace Lytec.Common;

public static partial class EnumerableUtils
{
    public static IEnumerable<T> GetEnumValues<T>() where T : struct, Enum
    => Enum.GetValues(typeof(T)).Cast<T>();

    public static IEnumerable<T> Distinct<T>(this IEnumerable<T> source, Func<T?, T?, bool> equals, Func<T, int>? getHashCode = null)
    => source.Distinct(new IEqualityComparerWrapper<T>(equals, getHashCode));

    public static IEnumerable<T> DistinctBy<T, TCompare>(this IEnumerable<T> source, Func<T, TCompare> selector)
    => source.Distinct(new IEqualityComparerWrapper<T>((a, b) => Comparer<TCompare>.Default.Compare(selector(a!), selector(b!)) == 0, x => selector(x)!.GetHashCode()));

    public static void DisposeAll<T>(this IEnumerable<T> source) where T : IDisposable
    {
        foreach (var i in source)
            i?.Dispose();
    }

    public static void Add<T>(this IList<T> source, IEnumerable<T> items)
    {
        foreach (var item in items)
            source.Add(item);
    }

    public static int IndexOf<T>(this IReadOnlyList<T> source, T item)
    {
        var index = source.TakeWhile(i => Comparer<T>.Default.Compare(i, item) != 0).Count();
        return index == source.Count ? -1 : index;
    }

    public static int IndexOf<T>(this IReadOnlyList<T> source, Predicate<T> comparer)
    {
        var index = source.TakeWhile(i => !comparer(i)).Count();
        return index == source.Count ? -1 : index;
    }

    private static int IndexOf<T>(IList<T> source, IReadOnlyList<T> data)
    {
        if (data.Count == 0)
            return -1;
        for (var i = 0; i + data.Count - 1 < source.Count; i++)
        {
            for (var j = 0; j < data.Count; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(source[i + j], data[j]))
                    break;
                if (j + 1 == data.Count)
                    return i;
            }
        }
        return -1;
    }

    private static int IndexOf<T>(IReadOnlyList<T> source, IList<T> data)
    {
        if (data.Count == 0)
            return -1;
        for (var i = 0; i + data.Count - 1 < source.Count; i++)
        {
            for (var j = 0; j < data.Count; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(source[i + j], data[j]))
                    break;
                if (j + 1 == data.Count)
                    return i;
            }
        }
        return -1;
    }

    private static int IndexOf<T>(IReadOnlyList<T> source, IReadOnlyList<T> data)
    {
        if (data.Count == 0)
            return -1;
        for (var i = 0; i + data.Count - 1 < source.Count; i++)
        {
            for (var j = 0; j < data.Count; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(source[i + j], data[j]))
                    break;
                if (j + 1 == data.Count)
                    return i;
            }
        }
        return -1;
    }

    private static int IndexOf<T>(IList<T> source, IList<T> data)
    {
        if (data.Count == 0)
            return -1;
        for (var i = 0; i + data.Count - 1 < source.Count; i++)
        {
            for (var j = 0; j < data.Count; j++)
            {
                if (!EqualityComparer<T>.Default.Equals(source[i + j], data[j]))
                    break;
                if (j + 1 == data.Count)
                    return i;
            }
        }
        return -1;
    }

    public static int IndexOf<T>(this IList<T> source, IEnumerable<T> data)
    {
        switch (data)
        {
            case IReadOnlyList<T> rd: return IndexOf(source, rd);
            case IList<T> d: return IndexOf(source, d);
            default: return IndexOf(source, (IReadOnlyList<T>)data.ToList());
        }
    }

    public static int IndexOf<T>(this IReadOnlyList<T> source, IEnumerable<T> data)
    {
        switch (data)
        {
            case IReadOnlyList<T> rd: return IndexOf(source, rd);
            case IList<T> d: return IndexOf(source, d);
            default: return IndexOf(source, (IReadOnlyList<T>)data.ToList());
        }
    }

    public static string JoinToString<T>(this IEnumerable<T> list, string sep = ",")
    => list.JoinToString(null, sep);

    public static string JoinToString<T>(this IEnumerable<T> list, Func<T, string>? toString, string sep = ",")
    => string.Join(sep, list.Select(toString ?? (t => t?.ToString() ?? "")));

}
