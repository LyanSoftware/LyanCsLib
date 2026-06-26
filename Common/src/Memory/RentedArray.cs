using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Memory;

public class RentedArray<T> : IDisposable
{
    public T[] Array { get; }
    public Span<T> Span => Array;
    public Memory<T> Memory => Array;
    public bool ClearOnReturn { get; set; } = false;
    private ArrayPool<T> Pool { get; }
    public RentedArray(int minSize, ArrayPool<T> pool = null)
    {
        Pool = pool ?? ArrayPool<T>.Shared;
        Array = Pool.Rent(minSize);
    }

    private bool IsDisposed;
    public void Dispose()
    {
        if (!IsDisposed)
        {
            IsDisposed = true;
            Pool.Return(Array, ClearOnReturn);
        }
    }

    public static implicit operator T[](RentedArray<T> arr) => arr.Array;
}
