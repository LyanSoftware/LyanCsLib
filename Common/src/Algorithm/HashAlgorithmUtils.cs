using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using Lytec.Common;

namespace Lytec.Common.Algorithm;

public static class HashAlgorithmUtils
{
    public static byte[] ComputeHash(this HashAlgorithm hash, byte[] bytes, int len)
    => hash.ComputeHash(bytes, 0, len);

    public static byte[] ComputeHash(this HashAlgorithm hash, IEnumerable<byte> bytes)
    => hash.ComputeHash(new EnumerableWrapperStream(bytes));

}
