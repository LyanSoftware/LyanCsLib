using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;

namespace Lytec.Common;

public static class CollectionExtensions
{
    public static IReadOnlyList<T> AsReadOnly<T>(this IList<T> list)
    {
        if (list is IReadOnlyList<T> lst)
            return lst;
        return new ReadOnlyCollection<T>(list);
    }
    public static IReadOnlyDictionary<TKey, TValue> AsReadOnly<TKey, TValue>(this IDictionary<TKey, TValue> list)
        where TKey: notnull
    {
        if (list is IReadOnlyDictionary<TKey, TValue> lst)
            return lst;
        return new ReadOnlyDictionary<TKey, TValue>(list);
    }
}
