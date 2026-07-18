using System.Security.Cryptography;
using Lytec.Common;
using Lytec.Common.Data;

namespace Lytec.Common.Algorithm;

public static partial class HashAlgorithmExtensions
{

    [GenerateHashAlgorithmExtensions]
    public static MD5 GetMd5() => MD5.Create();

    public static ushort ConvertCrc16_CCITT_XMODEMResult(byte[] bytes) => bytes.ToStruct<ushort>();
    [GenerateHashAlgorithmExtensions(nameof(ConvertCrc16_CCITT_XMODEMResult))]
    public static CheckSum.CRC16.CCITT_XMODEM GetCrc16_CCITT_XMODEM(ushort init = 0) => new CheckSum.CRC16.CCITT_XMODEM(init);

}
