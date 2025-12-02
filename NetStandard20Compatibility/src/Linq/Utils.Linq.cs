namespace System.Linq;

public static partial class NetStandard20Compatibility_Lytec
{
    public static HashSet<T> ToHashSet<T>(this IEnumerable<T> source)
    {
        var set = new HashSet<T>();
        foreach (var item in source)
            set.Add(item);
        return set;
    }
}
