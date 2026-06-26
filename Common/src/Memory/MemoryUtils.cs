using System;
using System.Buffers;
using System.Collections.Generic;
using System.Text;

namespace Lytec.Common.Memory;

public static class MemoryUtils
{
    public static RentedArray<T> RentWrapped<T>(this ArrayPool<T> pool, int minSize) => new RentedArray<T>(minSize, pool);
}
