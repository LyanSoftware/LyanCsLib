using System.Security.Cryptography;
using Org.BouncyCastle.Crypto.Digests;

namespace Lytec.Common.Crypto;

public class SM3 : HashAlgorithm
{
    protected SM3Digest Digest { get; set; } = new();

    public override void Initialize() => Digest = new();

    protected override void HashCore(byte[] array, int ibStart, int cbSize) => Digest.BlockUpdate(array, ibStart, cbSize);

    protected override byte[] HashFinal()
    {
        var buf = new byte[Digest.GetDigestSize()];
        Digest.DoFinal(buf, 0);
        return buf;
    }

    public static byte[] Compute(params byte[] data)
    {
        var d = new SM3Digest();
        d.BlockUpdate(data, 0, data.Length);
        var buf = new byte[d.GetDigestSize()];
        d.DoFinal(buf, 0);
        return buf;
    }

    public static byte[] Compute(IEnumerable<byte> data) => Compute(data.ToArray());
}
