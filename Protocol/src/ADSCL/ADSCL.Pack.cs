using System.Security.Cryptography;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common.Serialization;
using Lytec.Common;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
        public abstract class Pack<TImpl, TData> : IPackage, ISerializable<TImpl>
            where TImpl : Pack<TImpl, TData>, new()
            where TData : ISerializable<TData>, ICloneable<TData>, new()
        {
            public static byte[] SendIdentifier
            {
                get => _SendIdentifier ?? throw new NullReferenceException();
                set => _SendIdentifier = value;
            }
            static byte[] _SendIdentifier = Array.Empty<byte>();
            public static byte[] RecvIdentifier
            {
                get => _RecvIdentifier ?? throw new NullReferenceException();
                set => _RecvIdentifier = value;
            }
            static byte[] _RecvIdentifier = Array.Empty<byte>();

            public static int MinDataLength { get; set; }

            public static Endian DefaultEndian { get; set; } = Endian.Little;

            public static Func<CheckSum<ushort>> GetCRC16Algorithm { get; set; } = () => new CheckSum.CRC16.CCITT_XMODEM();

            public static IPasswordConverter<int> PasswordConverter { get; set; } = new PasswordConverter();

            public static int NoPassword { get; set; } = -1;
            public static int PasswordAccepted { get; set; } = 0;

            private static ushort NextPackIndex = 0;
            private static readonly object SyncRoot = new object();
            public static Func<ushort> GetNextPackIndex { get; set; } = () =>
            {
                ushort id;
                lock (SyncRoot)
                    id = ++NextPackIndex;
                return id;
            };

            public virtual byte[] Identifier { get; set; } = Array.Empty<byte>();
            public virtual bool IsSend => Identifier.SequenceEqual(SendIdentifier);
            public virtual bool IsRecv => Identifier.SequenceEqual(RecvIdentifier);

            public virtual byte AddrCode { get; set; }

            public virtual ushort PackIndex { get; set; }

            public virtual int Password { get; set; } = NoPassword;

            public virtual TData? Data { get; set; }

            public virtual ushort CheckSum { get; set; }

            public virtual bool IsValid => Data != null && (IsSend || IsRecv) && RecalcCheckSum() == CheckSum;

            public virtual bool IsMyAnswer(TImpl pack)
            => IsSend && pack.IsValid && pack.IsRecv
                && pack.AddrCode == AddrCode
                && pack.PackIndex == PackIndex
                && pack.Password == 0;

            public virtual bool IsPasswordAccepted => IsRecv && (Password == NoPassword || Password == PasswordAccepted);

            public virtual void UpdatePackIndex() => PackIndex = GetNextPackIndex();

            public virtual ushort RecalcCheckSum()
            {
                var buf = Serialize();
                return GetCRC16Algorithm().Compute(buf.Take(buf.Length - sizeof(ushort)));
            }

            public virtual void UpdateCheckSum() => CheckSum = RecalcCheckSum();

            public virtual byte[] Serialize()
            {
#pragma warning disable IDE0028 // 简化集合初始化
                var buf = new List<byte>();
                buf.Add(Identifier);
                buf.Add(AddrCode);
                buf.Add(PackIndex.ToBytes(DefaultEndian));
                buf.Add(Password.ToBytes(DefaultEndian));
                ushort dlen = 0;
                if (Data != null)
                {
                    var dbuf = Data.Serialize();
                    dlen = (ushort)dbuf.Length;
                    buf.Add(dlen.ToBytes(DefaultEndian));
                    buf.Add(dbuf);
                }
                else buf.Add(dlen.ToBytes(DefaultEndian));
                buf.Add(CheckSum.ToBytes(Endian.Big));
#pragma warning restore IDE0028 // 简化集合初始化
                return buf.ToArray();
            }

            public class Deserializer : IDeserializer<TImpl>, ISequenceDeserializer<TImpl>
            {
                public static readonly Deserializer Default = new Deserializer();

                public static Func<ITimer>? CreateAutoResetTimer { get; set; }

                public virtual ITimer? RecvTimeoutTimer { get; set; }

                public virtual int RecvTimeout { get; set; } = 500;

                protected virtual IList<byte> Cache { get; set; }
                protected virtual int Step { get; set; }
                protected virtual int StepLen { get; set; }

                public Deserializer(int recvTimeout = 500)
                {
                    RecvTimeout = recvTimeout;
                    Cache = new List<byte>();
                    Reset();
                    RecvTimeoutTimer = CreateAutoResetTimer?.Invoke();
                    if (RecvTimeoutTimer != null)
                    {
                        RecvTimeoutTimer.Interval = RecvTimeout;
                        RecvTimeoutTimer.OnTimer += () => Reset();
                    }
                }

                protected virtual void MoveStep(int step)
                {
                    StepLen = 0;
                    Step = step;
                }

                public virtual void Reset()
                {
                    RecvTimeoutTimer?.Stop();
                    Cache.Clear();
                    Step = 0;
                    StepLen = 0;
                }

                private enum Steps
                {
                    Identifier = 0,
                    AddrCode,
                    PackIndex,
                    Password,
                    DataLen,
                    Data,
                    CheckSum,
                    Fin
                }

                static readonly IReadOnlyDictionary<Steps, int> NoValidStepLens = new Dictionary<Steps, int>()
                {
                    { Steps.AddrCode  , 1 }, // 1字节地址码
                    { Steps.PackIndex , 2 }, // 2字节包序号
                    { Steps.Password  , 4 }, // 4字节访问密码
                    { Steps.DataLen   , 2 }, // 2字节数据长度
                    { Steps.CheckSum  , 2 }, // 2字节CRC16
                };

                private int DataLen = 0;
                public virtual TImpl? Deserialize(byte d)
                {
                    var RcvLen = Cache.Count;
                    var beforeRcvLen = RcvLen;
                    if (NoValidStepLens.TryGetValue((Steps)Step, out var len))
                    {
                        Cache.Add(d);
                        StepLen++;
                        if (StepLen >= len)
                        {
                            switch ((Steps)Step)
                            {
                                case Steps.DataLen:
                                    DataLen = Cache.Skip(Cache.Count - 2).ToArray().ToStruct<ushort>(DefaultEndian);
                                    if (DataLen < MinDataLength)
                                    {
                                        Reset();
                                        return null;
                                    }
                                    break;
                                case Steps.CheckSum:
                                    if (GetCRC16Algorithm().Compute(Cache) != 0)
                                    {
                                        Reset();
                                        return null;
                                    }
                                    break;
                            }
                            MoveStep(Step + 1);
                        }
                    }
                    else
                    {
                        switch ((Steps)Step)
                        {
                            default:
                                Reset();
                                return Deserialize(d);
                            case Steps.Identifier:
                                if (d == RecvIdentifier[RcvLen] || d == SendIdentifier[RcvLen])
                                {
                                    if (RcvLen == 0)
                                        RecvTimeoutTimer?.Start();
                                    Cache.Add(d);
                                    RcvLen++;
                                    StepLen++;
                                    if ((Cache[0] == RecvIdentifier[0] && StepLen >= RecvIdentifier.Length)
                                        || StepLen >= SendIdentifier.Length)
                                        MoveStep(Step + 1);
                                }
                                break;
                            case Steps.Data:
                                Cache.Add(d);
                                RcvLen++;
                                StepLen++;
                                if (StepLen >= DataLen)
                                    MoveStep(Step + 1);
                                break;
                        }
                    }
                    TImpl? p = null;
                    switch ((Steps)Step)
                    {
                        case Steps.Fin:
                            RecvTimeoutTimer?.Stop();
                            p = new TImpl();
                            var offset = 0;
                            p.Identifier = Cache.Take(Cache[offset] == RecvIdentifier[offset] ? RecvIdentifier.Length : SendIdentifier.Length).ToArray();
                            offset += p.Identifier.Length;
                            p.AddrCode = Cache[offset++];
                            p.PackIndex = Cache.ToStruct<ushort>(offset, DefaultEndian);
                            offset += sizeof(ushort);
                            p.Password = Cache.ToStruct<int>(offset, DefaultEndian);
                            offset += sizeof(int);
                            offset += sizeof(ushort); // DataLen
                            p.Data = DataLen > 0 ? ((IFactory<IDeserializer<TData>>)new TData()).Create().Deserialize(Cache.Skip(offset).Take(DataLen)) : default;
                            offset += DataLen;
                            p.CheckSum = Cache.ToStruct<ushort>(offset, Endian.Big);
                            Reset();
                            break;
                        default:
                            if (RcvLen > beforeRcvLen)
                                RecvTimeoutTimer?.Restart();
                            break;

                    }
                    return p;
                }
                public virtual TImpl? Deserialize(IEnumerable<byte> d, out int DeserializedLength)
                {
                    Reset();
                    DeserializedLength = 0;
                    foreach (var b in d)
                    {
                        var p = Deserialize(b);
                        DeserializedLength++;
                        if (p != null)
                            return p;
                    }
                    return null;
                }

                public virtual TImpl? Deserialize(IEnumerable<byte> b)
                => Deserialize(b, out _);

                public TImpl? Deserialize(byte data, out bool ok)
                {
                    var p = Deserialize(data);
                    ok = p != null;
                    return p;
                }

                public TImpl? Deserialize(IEnumerable<byte> data, out int DeserializedLength, out bool ok)
                {
                    var p = Deserialize(data, out DeserializedLength);
                    ok = p != null;
                    return p;
                }
            }

            public static Func<Deserializer> CreateDeserializer { get; set; } = () => new Deserializer();

            public static TImpl? Deserialize(byte d) => Deserializer.Default.Deserialize(d);
            public static TImpl? Deserialize(IEnumerable<byte> d, out int DeserializedLength) => CreateDeserializer().Deserialize(d, out DeserializedLength);

            IDeserializer<TImpl> IFactory<IDeserializer<TImpl>>.Create() => CreateDeserializer();
        }

        public class Pack : Pack<Pack, CommandPack>
        {
            static Pack()
            {
                SendIdentifier = new byte[7] { (byte)'\x1b', (byte)'$', (byte)'A', (byte)'d', (byte)'S', (byte)'c', (byte)'L' };
                RecvIdentifier = new byte[7] { (byte)'\x1b', (byte)'$', (byte)'a', (byte)'D', (byte)'s', (byte)'C', (byte)'l' };
                MinDataLength = CommandPack.MinDataLength;
            }
        }

        public class CommandPack : IPackage, ISerializable<CommandPack>, ICloneable<CommandPack>
        {
            public const int MinDataLength = 12;

            public virtual int Command { get; set; }
            public virtual int Arg1 { get; set; }
            public virtual int Arg2 { get; set; }
            public virtual byte[] Arg3 { get; set; } = Array.Empty<byte>();

            public CommandPack() { }
            public CommandPack(int command, int arg1, int arg2, byte[]? arg3 = null)
            {
                Command = command;
                Arg1 = arg1;
                Arg2 = arg2;
                Arg3 = arg3 ?? Array.Empty<byte>();
            }

            public virtual byte[] Serialize()
            => Command.ToBytes(Endian.Little)
                .Concat(Arg1.ToBytes(Endian.Little))
                .Concat(Arg2.ToBytes(Endian.Little))
                .Concat(Arg3)
                .ToArray();

            class Deserializer : IDeserializer<CommandPack>
            {
                public CommandPack? Deserialize(IEnumerable<byte> b)
                {
                    var cmd = b.Take(4).ToArray();
                    var arg1 = b.Skip(4).Take(4).ToArray();
                    var arg2 = b.Skip(8).Take(4).ToArray();
                    if (cmd.Length != 4
                        || arg1.Length != 4
                        || arg2.Length != 4)
                        return null;
                    b = b.Skip(12);
                    return new CommandPack()
                    {
                        Command = cmd.ToStruct<int>(Endian.Little),
                        Arg1 = arg1.ToStruct<int>(Endian.Little),
                        Arg2 = arg2.ToStruct<int>(Endian.Little),
                        Arg3 = b.ToArray(),
                    };
                }
            }
            static readonly Deserializer _Deserializer = new Deserializer();
            IDeserializer<CommandPack> IFactory<IDeserializer<CommandPack>>.Create() => CreateDeserializer();
            public virtual IDeserializer<CommandPack> CreateDeserializer() => _Deserializer;

            public CommandPack? Deserialize(byte[] bytes) => CreateDeserializer().Deserialize(bytes);

            public virtual CommandPack Clone() => new CommandPack(Command, Arg1, Arg2, Arg3?.ToArray());

            object ICloneable.Clone() => Clone();
        }

    }
}
