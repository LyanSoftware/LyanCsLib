
namespace Lytec.Common
{

#if NETSTANDARD2_1_OR_GREATER || NET
    public
#else
    internal
#endif
            static partial class Net20CompatibilityUtils
    {
        public static int Count(this Range r, int length) => r.GetOffsetAndLength(length).Length;
        public static int Count(this Range r)
        {
            if (r.Start.IsFromEnd || r.End.IsFromEnd)
                throw new NotSupportedException();
            return r.End.Value - r.Start.Value;
        }

        public static IEnumerable<int> AsEnumerable(this Range r, int length)
        {
            var (off, len) = r.GetOffsetAndLength(length);
            return Enumerable.Range(off, len);
        }
        public static IEnumerable<int> AsEnumerable(this Range r)
        {
            if (r.Start.IsFromEnd || r.End.IsFromEnd)
                throw new NotSupportedException();
            return Enumerable.Range(r.Start.Value, r.End.Value - r.Start.Value);
        }
    }
}
