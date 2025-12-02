using System;
using System.Collections.Generic;
using System.Text;

namespace System.Security.Cryptography;

public static class HashAlgorithmUtils
{
    public static byte[] ComputeHash(this HashAlgorithm hash, byte[] bytes, int len)
    => hash.ComputeHash(bytes, 0, len);

    public static byte[] ComputeHash(this HashAlgorithm hash, IEnumerable<byte> bytes)
    => hash.ComputeHash(bytes.ToArray());
}
