using System;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common
{
    public struct IEqualityComparerWrapper<T> : IEqualityComparer<T>
    {
        public new Func<T, T, bool> Equals { get; set; }
        public new Func<T, int>? GetHashCode { get; set; }

        public IEqualityComparerWrapper(Func<T, T, bool> equals, Func<T, int>? getHashCode = null)
        {
            Equals = equals;
            GetHashCode = getHashCode;
        }

        bool IEqualityComparer<T>.Equals(T x, T y) => Equals(x, y);

        int IEqualityComparer<T>.GetHashCode(T obj) => GetHashCode?.Invoke(obj) ?? 0;
    }

}
