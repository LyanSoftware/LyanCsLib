using System.Net;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using Lytec.Common.Data;

namespace Lytec.Common.Communication
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct IPv4Address
    {
        public IPAddress IPAddress => new IPAddress(Bytes);
        [field: Endian(Endian.Big)]
        public int Address { get; private set; }
        public byte[] Bytes => this.ToBytes();
        public IPv4Address(int address) => Address = address;
        public IPv4Address(byte[] bytes) => Address = bytes.ToStruct<IPv4Address>().Address;
        public IPv4Address(IPAddress address)
        => Address = address.AddressFamily == AddressFamily.InterNetwork ? address.GetAddressBytes().ToStruct<IPv4Address>().Address : throw new InvalidOperationException();
    }
}
