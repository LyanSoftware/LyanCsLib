using System.Collections.Generic;

namespace System.Linq
{
    public static class Enumerable_Lytec
    {
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source)
        {
            var set = new HashSet<TSource>();
            foreach (var item in source)
                set.Add(item);
            return set;
        }
        public static HashSet<TSource> ToHashSet<TSource>(this IEnumerable<TSource> source, IEqualityComparer<TSource> comparer)
        {
            var set = new HashSet<TSource>();
            foreach (var item in source)
            {
                if (!set.Contains(item, comparer))
                    set.Add(item);
            }
            return set;
        }
    }
}
