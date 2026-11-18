using System.Buffers;
using System.Runtime.InteropServices;
using Lytec.Common.Data;
using Lytec.Common.Serialization;
using Newtonsoft.Json;

namespace Lytec.Common.Number;

[Serializable]
[Endian(Endian.Little)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[FixedBinaryContract(SizeConst, typeof(CodecProvider))]
public struct WORDBool : IEquatable<WORDBool>, IBinarySerializable
{
    public const int SizeConst = 2;

    [JsonProperty(nameof(Value))]
    public bool SerializedValue { get => Value != 0; set => Value = (ushort)(value ? 1 : 0); }

    [JsonIgnore]
    public ushort Value { get; set; }

    public WORDBool(bool v) : this() => SerializedValue = v;

    public static implicit operator bool(WORDBool v) => v.SerializedValue;
    public static implicit operator WORDBool(bool v) => new WORDBool(v);

    public override string ToString() => ((bool)this).ToString();

    public static bool operator ==(WORDBool left, WORDBool right) => left.Equals(right);
    public static bool operator !=(WORDBool left, WORDBool right) => !(left == right);
    public static bool operator !(WORDBool v) => v.Value == 0;
    public override bool Equals(object? obj) => obj is WORDBool v && Equals(v);
    public bool Equals(WORDBool other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();
    
    public int SerializedSize => SizeConst;

    public byte[] Serialize(Endian? endian = null)
    {
        var d = new byte[SizeConst];
        FixedBinaryPrimitives.WriteUInt16(d, Value, FixedBinaryPrimitives.ResolveEndian(endian));
        return d;
    }

    public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? endian = null)
    {
        written = 0;
        if (destination.Length < SizeConst)
            return OperationStatus.DestinationTooSmall;
        FixedBinaryPrimitives.WriteUInt16(destination, Value, FixedBinaryPrimitives.ResolveEndian(endian));
        written = SizeConst;
        return OperationStatus.Done;
    }

    public sealed class CodecProvider : IFixedBinaryCodecProvider<WORDBool>
    {
        private static IFixedBinaryCodec<WORDBool> BECodec { get; } = new Codec(Endian.Big);
        private static IFixedBinaryCodec<WORDBool> LECodec { get; } = new Codec(Endian.Little);

        public IFixedBinaryCodec<WORDBool> GetBinaryCodec(Endian? endian = null)
        => FixedBinaryPrimitives.ResolveEndian(endian) == Endian.Big ? BECodec : LECodec;

        private sealed class Codec : IFixedBinaryCodec<WORDBool>
        {
            public int FixedSize => SizeConst;

            public int MinimumFrameLength => SizeConst;

            public int MaximumFrameLength => SizeConst;

            public Endian Endian { get; }

            public Codec(Endian endian) => Endian = endian;

            public BinaryResynchronizationMode ResynchronizationMode => BinaryResynchronizationMode.Automatic;

            public OperationStatus TryGetSerializedLength(WORDBool value, out int length)
            {
                length = SizeConst;
                return OperationStatus.Done;
            }

            public OperationStatus TrySerialize(WORDBool value, Span<byte> destination, out int written)
            => value.TrySerialize(destination, out written);

            public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out WORDBool value)
            {
                value = default;
                if (source.Length < SizeConst)
                    return isFinalBlock
                        ? BinaryParseResult.InvalidData(0, source.Length)
                        : BinaryParseResult.NeedMoreData(source.Length);

                value = new WORDBool
                {
                    Value = FixedBinaryPrimitives.ReadUInt16(source, Endian)
                };
                return BinaryParseResult.Done(SizeConst);
            }

            public IBinaryStreamDecoder<WORDBool> CreateStreamDecoder()
                => new FixedBinaryStreamDecoder<WORDBool>(this);
        }
    }
}
