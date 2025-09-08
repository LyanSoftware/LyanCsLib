using System.Security.Cryptography;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        public interface IPasswordConverter<T>
        {
            T Convert(IEnumerable<byte> data);
            T Convert(string data);
        }

        public class PasswordConverter : IPasswordConverter<int>
        {
            public int Convert(IEnumerable<byte> data) => new CheckSum.CRC16.CCITT_XMODEM().Compute(data);

            public int Convert(string data) => Convert(DefaultEncode.GetBytes(data));
        }

    }
}
