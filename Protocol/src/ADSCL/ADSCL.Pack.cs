using System.Security.Cryptography;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common.Serialization;
using Lytec.Common;
using Lytec.Common.Localization.Extensions;
using System.Buffers;
using System.Buffers.Binary;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        public enum PackDirection
        {
            ToDevice,
            FromDevice,
        }

        public class PackConfig
        {
            private const string LocalizeScope = "Lytec.Protocol.ADSCL.PackConfig";

            private readonly byte[] _ToDeviceIdentifier;
            public ReadOnlySpan<byte> ToDeviceIdentifier => _ToDeviceIdentifier;
            private readonly byte[] _FromDeviceIdentifier;
            public ReadOnlySpan<byte> FromDeviceIdentifier => _FromDeviceIdentifier;
            public int IdLength => ToDeviceIdentifier.Length;
            public int MinDataLength { get; }
            public int MaxDataLength { get; }

            public Endian DefaultEndian { get; init; } = Endian.Little;

            public Func<CheckSum<ushort>> CRC16AlgorithmFactory { get; init; } = () => new CheckSum.CRC16.CCITT_XMODEM();
            public CheckSum<ushort> GetCRC16Algorithm()
            => CRC16AlgorithmFactory?.Invoke()
                ?? throw new InvalidOperationException("Failed to get CRC algorithm instance")
                    .Localize(
                        LocalizeScope,
                        "GetCRCAlgorithmInstanceFailed",
                        "获取CRC算法实例失败"
                    );

            public IPasswordConverter<int> PasswordConverter { get; init; } = new PasswordConverter();

            public int NoPassword { get; init; } = -1;
            public int PasswordAccepted { get; init; } = 0;

            public PackConfig(IReadOnlyList<byte> toDeviceId, IReadOnlyList<byte> fromDeviceId, int minDataLen, int maxDataLen)
            {
                ArgumentNullException.ThrowIfNull(toDeviceId);
                ArgumentNullException.ThrowIfNull(fromDeviceId);
                ArgumentOutOfRangeException.ThrowIfNegative(minDataLen);
                ArgumentOutOfRangeException.ThrowIfNegative(maxDataLen);
                ArgumentOutOfRangeException.ThrowIfLessThan(maxDataLen, minDataLen);
                if (toDeviceId.Count != fromDeviceId.Count)
                    throw new ArgumentException("toDeviceId and fromDeviceId must be the same length", nameof(toDeviceId))
                        .Localize(
                            LocalizeScope,
                            "IdentifierLengthMismatch",
                            "toDeviceId 与 fromDeviceId 长度必须一致！"
                        );
                if (toDeviceId.Count == 0)
                    throw new ArgumentException("Identifier cannot be empty!", nameof(toDeviceId))
                        .Localize(
                            LocalizeScope,
                            "EmptyIdentifierError",
                            "Identifier(引导串/固定包头)不可为空！"
                        );
                if (toDeviceId.SequenceEqual(fromDeviceId))
                    throw new ArgumentException("toDeviceId and fromDeviceId must differ", nameof(toDeviceId))
                        .Localize(
                            LocalizeScope,
                            "IdentifierLengthMismatch",
                            "toDeviceId 与 fromDeviceId 不可相同！"
                        );
                _ToDeviceIdentifier = [.. toDeviceId];
                _FromDeviceIdentifier = [.. fromDeviceId];
                MinDataLength = minDataLen;
                MaxDataLength = maxDataLen;
            }

            public void Valid()
            {
                switch (DefaultEndian)
                {
                    case Endian.Little:
                    case Endian.Big:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(DefaultEndian), "Invalid Endianness");
                }
            }
        }

        public class DeviceContext<TPack, TData>
            where TPack : Pack<TPack, TData>
        {
            public virtual ISendAndGetAnswerConfig CommConfig { get; set; }
            public virtual IPackCodec<TPack, TData> Codec { get; set; }
            public virtual PackConfig Config => Codec.Config;
            public virtual byte AddrCode => (byte)(CommConfig.AddrCode ?? 0);
            public virtual int Password { get; set; }
            public virtual bool UseGlobalPackIndex { get; set; } = true;

            public DeviceContext(
                ISendAndGetAnswerConfig commCfg,
                IPackCodec<TPack, TData> codec,
                int password
            )
            {
                CommConfig = commCfg;
                Codec = codec;
                Password = password;
            }

            public virtual void SetPassword(string pw) => Password = Config.PasswordConverter.Convert(pw);
            public virtual void SetPassword(byte[] pw) => Password = Config.PasswordConverter.Convert(pw);
            public virtual void UnsetPassword() => Password = Config.NoPassword;

            private int NextPackIndex = 0;
            protected virtual ushort GetNextPackIndex() => (ushort)Interlocked.Increment(ref NextPackIndex);

            private static int NextPackIndexStatic = 0;
            protected static ushort GetNextPackIndexStatic() => (ushort)Interlocked.Increment(ref NextPackIndexStatic);

            public virtual TPack CreateRequest(TData data)
            => Codec.CreatePack(
                PackDirection.ToDevice,
                AddrCode,
                UseGlobalPackIndex ? GetNextPackIndexStatic() : GetNextPackIndex(),
                Password,
                data
            );

            public virtual bool IsPasswordAccepted(TPack pack)
            => pack.IsRecv && (pack.Password == Config.PasswordAccepted);
        }

        public class Pack<TPack, TData> where TPack: Pack<TPack, TData>
        {
            public PackDirection Direction { get; }
            public byte AddrCode { get; }
            public ushort PackIndex { get; }
            public int Password { get; }
            public TData Data { get; }

            public Pack(
                PackDirection direction,
                byte addrCode,
                ushort packIndex,
                int password,
                TData data
            )
            {
                Direction = direction;
                AddrCode = addrCode;
                PackIndex = packIndex;
                Password = password;
                Data = data;
            }

            public virtual bool IsSend => Direction == PackDirection.ToDevice;
            public virtual bool IsRecv => Direction == PackDirection.FromDevice;

            public virtual bool IsMyAnswer(TPack pack)
            => IsSend && pack.IsRecv
                && AddrCode == pack.AddrCode
                && PackIndex == pack.PackIndex;
        }

        public delegate TPack PackFactory<TPack, TData>(
            PackDirection direction,
            byte addrCode,
            ushort packIndex,
            int password,
            TData data
        ) where TPack: Pack<TPack, TData>;

        public interface IPackCodec<TPack, TData>
            : IBinaryCodec<TPack>
            where TPack: Pack<TPack, TData>
        {
            PackConfig Config { get; }
            TPack CreatePack(
                PackDirection direction,
                byte addrCode,
                ushort packIndex,
                int password,
                TData data
            );
        }

        public sealed class PackCodec<TPack, TData>
            : IPackCodec<TPack, TData>
            where TPack : Pack<TPack, TData>
        {
            private const string LocalizeScope = "Lytec.Protocol.ADSCL.PackCodec";

            public PackConfig Config { get; }
            public BinaryResynchronizationMode ResynchronizationMode { get; }
            public IBinarySerializer<TData> DataSerializer { get; }
            public IBinaryFrameParser<TData> DataDeserializer { get; }
            public PackFactory<TPack, TData> PackFactory { get; set; }
            public TPack CreatePack(
                PackDirection dir,
                byte addr,
                ushort pid,
                int pw,
                TData data
            )
            => PackFactory?.Invoke(dir, addr, pid, pw, data)
                ?? throw new InvalidOperationException("Failed to create Pack instance")
                    .Localize(
                        LocalizeScope,
                        "CreatePackInstanceFailed",
                        "创建Pack实例失败"
                    );

            public int OuterSize { get; }

            public int MinDataLen { get; }
            public int MinimumFrameLength { get; }
            public int MaxDataLen { get; }
            public int MaximumFrameLength { get; }

            static NotSupportedException PackageTooLongException()
            => new NotSupportedException("Package too long")
                .Localize(
                    LocalizeScope,
                    "PackageTooLong",
                    "数据包太长"
                );
            static InvalidOperationException DataSerializerMisbehavedError_InvalidOpStatus(string from, OperationStatus status)
            => new InvalidOperationException($"Serializer misbehaved: DataSerializer.{from} returned operation status {status}.")
                .Localize(
                    LocalizeScope,
                    "DataSerializerMisbehavedError_InvalidOpStatus",
                    "序列化器产生了不正确的行为： DataSerializer.{{From}} 返回了 {{Status}}",
                    ("From", from),
                    ("Status", status)
                );

            public PackCodec(
                PackConfig config,
                BinaryResynchronizationMode mode,
                IBinarySerializer<TData> dataSerializer,
                IBinaryFrameParser<TData> dataDeserializer,
                PackFactory<TPack, TData> packFactory
            )
            {
                ArgumentNullException.ThrowIfNull(config);
                ArgumentNullException.ThrowIfNull(dataSerializer);
                ArgumentNullException.ThrowIfNull(dataDeserializer);
                ArgumentNullException.ThrowIfNull(packFactory);
                config.Valid();
                ArgumentOutOfRangeException.ThrowIfGreaterThan(config.MinDataLength, ushort.MaxValue);

                Config = config;
                ResynchronizationMode = mode;
                DataSerializer = dataSerializer;
                DataDeserializer = dataDeserializer;
                PackFactory = packFactory;

                // 完整包：引导串 + 1字节地址 + 2字节包序号 + 4字节访问密码 + 2字节数据长度 + 数据（4字节命令码 + 8~N字节命令参数） + 2字节CRC校验。
                OuterSize = Config.ToDeviceIdentifier.Length + (1 + 2 + 4 + 2 + 2);
                ArgumentOutOfRangeException.ThrowIfLessThan(dataSerializer.MaximumFrameLength, dataSerializer.MinimumFrameLength, "dataSerializer.MaximumFrameLength");
                ArgumentOutOfRangeException.ThrowIfLessThan(dataDeserializer.MaximumFrameLength, dataDeserializer.MinimumFrameLength, "dataDeserializer.MaximumFrameLength");
                MinDataLen = Math.Max(Config.MinDataLength,
                                    Math.Max(dataSerializer.MinimumFrameLength, dataDeserializer.MinimumFrameLength));
                MaxDataLen = Math.Min(Math.Min(ushort.MaxValue, Config.MaxDataLength),
                                    Math.Min(dataSerializer.MaximumFrameLength, dataDeserializer.MaximumFrameLength));
                if (MinDataLen > MaxDataLen)
                    throw PackageTooLongException();
                MinimumFrameLength = OuterSize + MinDataLen;
                MaximumFrameLength = OuterSize + MaxDataLen;
            }

            public DeviceContext<TPack, TData> MakeContext(ISendAndGetAnswerConfig cfg)
            => new(cfg, this, Config.NoPassword);

            public DeviceContext<TPack, TData> MakeContext(ISendAndGetAnswerConfig cfg, int password)
            => new(cfg, this, password);

            public DeviceContext<TPack, TData> MakeContext(ISendAndGetAnswerConfig cfg, string password)
            => new(cfg, this, Config.PasswordConverter.Convert(password));

            public DeviceContext<TPack, TData> MakeContext(ISendAndGetAnswerConfig cfg, byte[] password)
            => new(cfg, this, Config.PasswordConverter.Convert(password));

            OperationStatus ValidAndTryGetDataLen(TPack value, out int len)
            {
                len = 0;
                if (value == null)
                    return OperationStatus.InvalidData;
                switch (value.Direction)
                {
                    case PackDirection.ToDevice:
                    case PackDirection.FromDevice:
                        break;
                    default:
                        return OperationStatus.InvalidData;
                }

                var st = DataSerializer.TryGetSerializedLength(value.Data, out len);
                if (st != OperationStatus.Done)
                {
                    len = 0;
                    if (st == OperationStatus.InvalidData)
                        return OperationStatus.InvalidData;
                    throw DataSerializerMisbehavedError_InvalidOpStatus(
                        nameof(DataSerializer.TryGetSerializedLength),
                        st
                    );
                }
                if (len < MinDataLen || len > MaxDataLen)
                    return OperationStatus.InvalidData;
                return OperationStatus.Done;
            }
            public OperationStatus TryGetSerializedLength(TPack value, out int len)
            {
                var st = ValidAndTryGetDataLen(value, out len);
                if (st != OperationStatus.Done)
                    return st;
                len += OuterSize;
                return OperationStatus.Done;
            }

            public OperationStatus TrySerialize(TPack value, Span<byte> dst, out int written)
            {
                written = 0;
                var st = ValidAndTryGetDataLen(value, out var dlen1);
                if (st != OperationStatus.Done)
                    return st;
                ushort dlen = (ushort)dlen1;
                if (dst.Length < (dlen + OuterSize))
                    return OperationStatus.DestinationTooSmall;
                var queuesz = 0;
                (int off, int len) enqueue(int len)
                {
                    var d = (queuesz, len);
                    queuesz += len;
                    return d;
                }
                var id = enqueue(Config.IdLength);
                var addr = enqueue(1);
                var pid = enqueue(2);
                var pw = enqueue(4);
                var datlen = enqueue(2);
                var dat = enqueue(dlen);
                st = DataSerializer.TrySerialize(value.Data, dst.Slice(dat.off, dat.len), out var len);
                switch (st)
                {
                    case OperationStatus.Done:
                        break;
                    case OperationStatus.InvalidData:
                        return st;
                    case OperationStatus.DestinationTooSmall:
                        throw new InvalidOperationException("DataSerializer misbehaved: Attempted to write data beyond measured size")
                            .Localize(
                                LocalizeScope,
                                "DataSerializerMisbehavedError_OutOfBoundsWrite",
                                "序列化器产生了不正确的行为： 要求写出超出测量大小的数据"
                            );
                    default:
                    case OperationStatus.NeedMoreData:
                        throw DataSerializerMisbehavedError_InvalidOpStatus(
                            nameof(DataSerializer.TrySerialize),
                            st
                        );
                }
                if (dlen != len)
                    throw new InvalidOperationException($"Serialization length changed: {dlen} -> {len}")
                        .Localize(
                            LocalizeScope,
                            "SerializationLengthChangedError",
                            "序列化后的数据长度发生了变化：{{BeforeLength}} -> {{AfterLength}}",
                            ("BeforeLength", dlen),
                            ("AfterLength", len)
                        );

                (value.IsSend ? Config.ToDeviceIdentifier : Config.FromDeviceIdentifier)
                    .CopyTo(dst.Slice(id.off, id.len));
                FixedBinaryPrimitives.Write(dst.Slice(addr.off, addr.len), value.AddrCode, Config.DefaultEndian);
                FixedBinaryPrimitives.Write(dst.Slice(pid.off, pid.len), value.PackIndex, Config.DefaultEndian);
                FixedBinaryPrimitives.Write(dst.Slice(pw.off, pw.len), value.Password, Config.DefaultEndian);
                FixedBinaryPrimitives.Write(dst.Slice(datlen.off, datlen.len), dlen, Config.DefaultEndian);
                using var crc = Config.GetCRC16Algorithm();
                foreach (var b in dst[..queuesz])
                    crc.Compute(b);
                FixedBinaryPrimitives.Write(dst.Slice(queuesz, 2), crc.Result, Endian.Big);
                written = queuesz + 2;
                return OperationStatus.Done;
            }

            public IBinaryStreamDecoder<TPack> CreateStreamDecoder()
            => new BufferedBinaryStreamDecoder<TPack>(this, ResynchronizationMode);

            public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out TPack value)
            {
                BinaryParseResult parse(ReadOnlySpan<byte> src, out TPack data)
                {
                    data = default!;
                    PackDirection dir;
                    var rlen = 0;
                    {
                        var id = src.Length >= Config.IdLength ? src[..Config.IdLength] : src;
                        var isSend = id.SequenceEqual(Config.ToDeviceIdentifier[..id.Length]);
                        var isRecv = !isSend && id.SequenceEqual(Config.FromDeviceIdentifier[..id.Length]);
                        if (!isSend && !isRecv)
                            return BinaryParseResult.InvalidData(1, 1);
                        rlen += id.Length;
                        if (id.Length < Config.IdLength)
                            return BinaryParseResult.NeedMoreData(rlen);
                        dir = isSend ? PackDirection.ToDevice : PackDirection.FromDevice;
                    }
                    if (src.Length < OuterSize-2) // 除去末尾的CRC, 包含数据长度
                        return BinaryParseResult.NeedMoreData(rlen);
                    var addr = src[rlen++];
                    var pid = FixedBinaryPrimitives.ReadUInt16(src[rlen..], Config.DefaultEndian);
                    rlen += 2;
                    var pw = FixedBinaryPrimitives.ReadInt32(src[rlen..], Config.DefaultEndian);
                    rlen += 4;
                    var dlen = FixedBinaryPrimitives.ReadUInt16(src[rlen..], Config.DefaultEndian);
                    rlen += 2;
                    if (dlen < MinDataLen || dlen > MaxDataLen)
                        return BinaryParseResult.InvalidData(Config.IdLength, rlen);
                    var plen = dlen + OuterSize;
                    if (plen > MaximumFrameLength)
                        return BinaryParseResult.InvalidData(Config.IdLength, rlen);
                    if (plen > src.Length)
                        return BinaryParseResult.NeedMoreData(rlen);
                    var dat = src.Slice(rlen, dlen);
                    rlen += dlen;
                    rlen += 2;
                    using var crc = Config.GetCRC16Algorithm();
                    foreach (var b in src[..rlen])
                        crc.Compute(b);
                    if (crc.Result != 0)
                        return BinaryParseResult.InvalidData(Config.IdLength, rlen);
                    var st = DataDeserializer.Parse(dat, true, out var d);
                    if (st.Status != OperationStatus.Done || st.Consumed != dlen)
                        return BinaryParseResult.InvalidData(Config.IdLength, rlen);
                    data = CreatePack(dir, addr, pid, pw, d);
                    return BinaryParseResult.Done(rlen);
                }
                var ret = parse(source, out value);
                if (isFinalBlock)
                {
                    var srclen = source.Length;
                    void err() => ret = BinaryParseResult.InvalidData(0, srclen);
                    switch (ret.Status)
                    {
                        case OperationStatus.NeedMoreData:
                            err();
                            break;
                        case OperationStatus.Done:
                            if (ret.Consumed != source.Length)
                                err();
                            break;
                    }
                    if (ret.Status == OperationStatus.InvalidData)
                        value = default!;
                }
                return ret;
            }
        }

        public class DeviceContext : DeviceContext<Pack, CommandPack>
        {
            public DeviceContext(ISendAndGetAnswerConfig commCfg, IPackCodec<Pack, CommandPack> codec, int password)
                : base(commCfg, codec, password)
            {
            }
        }

        public class Pack : Pack<Pack, CommandPack>
        {
            public Pack(PackDirection direction, byte addrCode, ushort packIndex, int password, CommandPack data)
                : base(direction, addrCode, packIndex, password, data)
            {
            }

            public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg)
            => new(cfg, Codec, Config.NoPassword);

            public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, int password)
            => new(cfg, Codec, password);

            public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, string password)
            => new(cfg, Codec, Config.PasswordConverter.Convert(password));

            public static DeviceContext MakeContext(ISendAndGetAnswerConfig cfg, byte[] password)
            => new(cfg, Codec, Config.PasswordConverter.Convert(password));

            public static PackConfig Config { get; } = new(
                [(byte)'\x1b', (byte)'$', (byte)'A', (byte)'d', (byte)'S', (byte)'c', (byte)'L'],
                [(byte)'\x1b', (byte)'$', (byte)'a', (byte)'D', (byte)'s', (byte)'C', (byte)'l'],
                CommandPack.MinDataLength,
                CommandPack.MinDataLength+1024
            );

            public static PackCodec<Pack, CommandPack> Codec { get; } = new(
                Config,
                BinaryResynchronizationMode.Automatic,
                new CommandPackSerializer(),
                new CommandPackDeserializer(),
                (dir, addr, pid, pw, data) => new Pack(dir, addr, pid, pw, data)
            );
            
            public class CommandPackSerializer : IBinarySerializer<CommandPack>
            {
                public int MinimumFrameLength => Config.MinDataLength;

                public int MaximumFrameLength => Config.MaxDataLength;

                public OperationStatus TryGetSerializedLength(CommandPack value, out int length)
                {
                    length = 0;
                    var arg3 = value.Arg3 ?? [];
                    if (arg3.Length > (Config.MaxDataLength-12))
                        return OperationStatus.InvalidData;
                    var len = 12 + arg3.Length;
                    if (len > Config.MaxDataLength)
                        return OperationStatus.InvalidData;
                    length = len;
                    return OperationStatus.Done;
                }

                public OperationStatus TrySerialize(CommandPack value, Span<byte> dst, out int written)
                {
                    ArgumentNullException.ThrowIfNull(value);
                    var arg3 = value.Arg3 ?? [];
                    written = 0;
                    var st = TryGetSerializedLength(value, out var dlen);
                    if (st != OperationStatus.Done)
                        return st;
                    if (dst.Length < dlen)
                        return OperationStatus.DestinationTooSmall;
                    FixedBinaryPrimitives.Write(dst.Slice(0, 4), value.Command, Config.DefaultEndian);
                    FixedBinaryPrimitives.Write(dst.Slice(4, 4), value.Arg1, Config.DefaultEndian);
                    FixedBinaryPrimitives.Write(dst.Slice(8, 4), value.Arg2, Config.DefaultEndian);
                    arg3.AsReadOnlySpan().CopyTo(dst[12..]);
                    written = dlen;
                    return OperationStatus.Done;
                }
            }
            public class CommandPackDeserializer : IBinaryFrameParser<CommandPack>
            {
                public int MinimumFrameLength => Config.MinDataLength;

                public int MaximumFrameLength => Config.MaxDataLength;

                public BinaryParseResult Parse(ReadOnlySpan<byte> source, bool isFinalBlock, out CommandPack value)
                {
                    value = default!;
                    if (source.Length > MaximumFrameLength)
                        return BinaryParseResult.InvalidData(source.Length - MaximumFrameLength, source.Length);
                    if (source.Length < MinimumFrameLength && isFinalBlock)
                        return BinaryParseResult.InvalidData(0, source.Length);
                    if (!isFinalBlock)
                        return BinaryParseResult.NeedMoreData(source.Length);
                    var cmd = FixedBinaryPrimitives.ReadInt32(source.Slice(0, 4), Config.DefaultEndian);
                    var arg1 = FixedBinaryPrimitives.ReadInt32(source.Slice(4, 4), Config.DefaultEndian);
                    var arg2 = FixedBinaryPrimitives.ReadInt32(source.Slice(8, 4), Config.DefaultEndian);
                    value = new(cmd, arg1, arg2, source[12..].ToArray());
                    return BinaryParseResult.Done(source.Length);
                }
            }
        }

        public class CommandPack : ICloneable<CommandPack>
        {
            public const int MinDataLength = 12;

            public virtual int Command { get; set; }
            public virtual int Arg1 { get; set; }
            public virtual int Arg2 { get; set; }
            public virtual byte[] Arg3 { get; set; } = [];

            public CommandPack() { }
            public CommandPack(int command, int arg1, int arg2, byte[]? arg3 = null)
            {
                Command = command;
                Arg1 = arg1;
                Arg2 = arg2;
                Arg3 = arg3 ?? [];
            }

            public virtual CommandPack Clone() => new CommandPack(Command, Arg1, Arg2, Arg3?.ToArray());

            object ICloneable.Clone() => Clone();
        }

    }
}
