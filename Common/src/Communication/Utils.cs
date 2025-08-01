using System.Net;
using System.Text.RegularExpressions;

namespace Lytec.Common.Communication
{
    public static class Utils
    {
        public static IPEndPoint EmptyIPEndPoint { get; } = new IPEndPoint(IPAddress.Any, 0);
        public static IPEndPoint EmptyIPv6EndPoint { get; } = new IPEndPoint(IPAddress.IPv6Any, 0);

        public static bool IsEmpty(this IPEndPoint ep) => ep.Equals(EmptyIPEndPoint) || ep.Equals(EmptyIPv6EndPoint);

        public static readonly Regex BaudrateRegex = new Regex("^[0-9]{1,9}$", RegexOptions.Compiled);

        public static readonly Regex NetPortRegex = new Regex("^[0-9]{1,5}$", RegexOptions.Compiled);

        public static readonly Regex IPv4AddressRegex = new Regex("^[0-2]?[0-9]{0,2}(?:(?<=[0-9])\\.[0-2]?[0-9]{0,2}){0,3}$", RegexOptions.Compiled);

        public static readonly Regex IPv6AddressRegex = new Regex("^[0-9a-fA-F]{0,4}(?:(?<=[0-9a-fA-F]):[0-9a-fA-F]{0,4}){0,7}$", RegexOptions.Compiled);
        // IPv6 Address Test
        //yip 1:2:3:4:5:6:7:8888
        //yip ::
        //yip 1::
        //yip 1::8
        //yip 1::7:8
        //yip 1::6:7:8
        //yip 1::5:6:7:8
        //yip 1::4:5:6:7:8
        //yip 1::3:4:5:6:7:8
        //yip ::8
        //yip 1:2::8
        //yip 1:2:3::8
        //yip 1:2:3:4::8
        //yip 1:2:3:4:5::8
        //yip 1:2:3:4:5:6::8
        //yip ::2:3:4:5:6:7:8
        //yip 1::3:4:5:6:7:8
        //yip 1:2::4:5:6:7:8
        //yip 1:2:3::5:6:7:8
        //yip 1:2:3:4::6:7:8
        //yip 1:2:3:4:5::7:8
        //yip 1:2:3:4:5:6::8
        //yip 1:2:3:4:5:6:7::
        //yip fe80::0202:B3FF:FE1E:8329
        //yip fe80::7:8%eth0
        //yip fe80::7:8%1
        //yip ::255.255.255.255
        //nip ::ffff:255.255.255.255
        //yip ::ffff:0:255.255.255.255
        //yip 2001:db8:3:4::192.0.2.33
        //yip 64:ff9b::192.0.2.33
        //yip FE80:0000:0000:0000:0202:B3FF:FE1E:8329

        //nip 1::2::3
        //nip ::256.0.0.0
        //nip 2001:bobbydavro::1
        //nip ::ffff::255.255.255.255

        public static bool ValidateUartBaudrate(string baudrate) => BaudrateRegex.IsMatch(baudrate);

        public static bool ValidateNetPort(string port) => NetPortRegex.IsMatch(port) && Convert.ToInt32(port) < 65536;

        public static bool ValidateNetAddress(string addr) => IPAddress.TryParse(addr, out _);

        public static bool ValidateIPv4Address(string addr) => IPv4AddressRegex.IsMatch(addr);

    }
}
