using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common.Serialization;
using Newtonsoft.Json;
using System.Buffers;
using System.Runtime.InteropServices;

namespace Lytec.Common.Number;

[Serializable]
[Endian(Endian.Little)]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[FixedBinaryContract(SizeConst, typeof(CodecProvider))]
public struct DWORDBool : IEquatable<DWORDBool>, IBinarySerializable
{
    public const int SizeConst = 4;

    [JsonProperty(nameof(Value))]
    public bool SerializedValue { get => Value != 0; set => Value = value ? 1 : 0; }

    [JsonIgnore]
    public int Value { get; set; }

    public DWORDBool(bool v) : this() => SerializedValue = v;

    public static implicit operator bool(DWORDBool v) => v.SerializedValue;
    public static implicit operator DWORDBool(bool v) => new DWORDBool(v);

    public override string ToString() => ((bool)this).ToString();

    public static bool operator ==(DWORDBool left, DWORDBool right) => left.Equals(right);
    public static bool operator !=(DWORDBool left, DWORDBool right) => !(left == right);
    public static bool operator !(DWORDBool v) => v.Value == 0;
    public override bool Equals(object? obj) => obj is DWORDBool v && Equals(v);
    public bool Equals(DWORDBool other) => Value == other.Value;
    public override int GetHashCode() => Value.GetHashCode();

    public int SerializedSize => SizeConst;

    public byte[] Serialize(Endian? endian = null)
    {
        var d = new byte[SizeConst];
        FixedBinaryPrimitives.WriteInt32(d, Value, FixedBinaryPrimitives.ResolveEndian(endian));
        return d;
    }

    public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? endian = null)
    {
        written = 0;
        if (destination.Length < SizeConst)
            return OperationStatus.DestinationTooSmall;
        FixedBinaryPrimitives.WriteInt32(destination, Value, FixedBinaryPrimitives.ResolveEndian(endian));
        written = SizeConst;
        return OperationStatus.Done;
    }

    public sealed class CodecProvider : IFixedBinaryCodecProvider<DWORDBool>
    {
        private static IFixedBinaryCodec<DWORDBool> BECodec { get; } = new Codec(Endian.Big);
        private static IFixedBinaryCodec<DWORDBool> LECodec { get; } = new Codec(Endian.Little);

        public IFixedBinaryCodec<DWORDBool> GetBinaryCodec(Endian? endian = null)
        => FixedBinaryPrimitives.ResolveEndian(endian) == Endian.Big ? BECodec : LECodec;

        private sealed class Codec : IFixedBinaryCodec<DWORDBool>
        {
            public int FixedSize => SizeConst;

            public int MinimumFrameLength => SizeConst;

            public int MaximumFrameLength => SizeConst;

            public Endian Endian { get; }

            public Codec(Endian endian) => Endian = endian;

            public BinaryResynchronizationMode ResynchronizationMode => BinaryResynchronizationMode.Automatic;

            public OperationStatus TryGetSerializedLength(DWORDBool value, out int length)
            {
                length = SizeConst;
                return OperationStatus.Done;
            }

            public OperationStatus TrySerialize(DWORDBool value, Span<byte> destination, out int written)
            => value.TrySerialize(destination, out written);

            public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out DWORDBool value)
            {
                value = default;
                if (source.Length < SizeConst)
                    return isFinalBlock
                        ? BinaryParseResult.InvalidData(0, source.Length)
                        : BinaryParseResult.NeedMoreData(source.Length);

                value = new DWORDBool
                {
                    Value = FixedBinaryPrimitives.ReadInt32(source, Endian)
                };
                return BinaryParseResult.Done(SizeConst);
            }

            public IBinaryStreamDecoder<DWORDBool> CreateStreamDecoder()
                => new FixedBinaryStreamDecoder<DWORDBool>(this);
        }
    }
}
