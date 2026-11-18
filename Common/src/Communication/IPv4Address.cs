using System.Net;
using System.Runtime.InteropServices;
using System.Net.Sockets;
using Lytec.Common.Data;
using Lytec.Common.Serialization;
using System.Buffers;

namespace Lytec.Common.Communication;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
[FixedBinaryContract(SizeConst, typeof(CodecProvider))]
public struct IPv4Address : IBinarySerializable, IComparable<IPv4Address>, IEquatable<IPv4Address>
{
    public const int SizeConst = 4;

    public IPAddress IPAddress => new IPAddress(Bytes);
    [field: Endian(Endian.Big)]
    public int Address { get; private set; }
    public byte[] Bytes => this.ToBytes();

    public int SerializedSize => SizeConst;

    public IPv4Address(int address) => Address = address;
    public IPv4Address(byte[] bytes) => Address = bytes.ToStruct<IPv4Address>().Address;
    public IPv4Address(IPAddress address)
    => Address = address.AddressFamily == AddressFamily.InterNetwork ? address.GetAddressBytes().ToStruct<IPv4Address>().Address : throw new InvalidOperationException();
    public override string ToString() => IPAddress.ToString();

    public byte[] Serialize(Endian? _ = null)
    {
        var d = new byte[SizeConst];
        FixedBinaryPrimitives.WriteInt32(d, Address, Endian.Big);
        return d;
    }

    public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? _ = null)
    {
        written = 0;
        if (destination.Length < SizeConst)
            return OperationStatus.DestinationTooSmall;
        FixedBinaryPrimitives.WriteInt32(destination, Address, Endian.Big);
        written = SizeConst;
        return OperationStatus.Done;
    }

    public int CompareTo(IPv4Address other) => Address.CompareTo(other.Address);

    public override bool Equals(object? obj) => obj is IPv4Address address && Equals(address);

    public bool Equals(IPv4Address other) => Address == other.Address;

    public override int GetHashCode() => HashCode.Combine(Address);

    public static implicit operator IPAddress(IPv4Address addr) => addr.IPAddress;
    public static implicit operator IPv4Address(IPAddress addr) => new(addr);

    public static bool operator ==(IPv4Address left, IPv4Address right) => left.Equals(right);

    public static bool operator !=(IPv4Address left, IPv4Address right) => !(left == right);

    public sealed class CodecProvider : IFixedBinaryCodecProvider<IPv4Address>
    {
        private static IFixedBinaryCodec<IPv4Address> CodecInstance { get; } = new Codec();

        public IFixedBinaryCodec<IPv4Address> GetBinaryCodec(Endian? _ = null) => CodecInstance;

        private sealed class Codec : IFixedBinaryCodec<IPv4Address>
        {
            public int FixedSize => SizeConst;

            public int MinimumFrameLength => SizeConst;

            public int MaximumFrameLength => SizeConst;

            public BinaryResynchronizationMode ResynchronizationMode => BinaryResynchronizationMode.Automatic;

            public OperationStatus TryGetSerializedLength(IPv4Address value, out int length)
            {
                length = SizeConst;
                return OperationStatus.Done;
            }

            public OperationStatus TrySerialize(IPv4Address value, Span<byte> destination, out int written)
            => value.TrySerialize(destination, out written);

            public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out IPv4Address value)
            {
                value = default;
                if (source.Length < SizeConst)
                    return isFinalBlock
                        ? BinaryParseResult.InvalidData(0, source.Length)
                        : BinaryParseResult.NeedMoreData(source.Length);

                value = new IPv4Address(FixedBinaryPrimitives.ReadInt32(source, Endian.Big));
                return BinaryParseResult.Done(SizeConst);
            }

            public IBinaryStreamDecoder<IPv4Address> CreateStreamDecoder()
                => new FixedBinaryStreamDecoder<IPv4Address>(this);
        }
    }

}
