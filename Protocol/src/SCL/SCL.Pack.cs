using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.SCL.Constants;
using Lytec.Common;
using System.Text;
using System.Data;

namespace Lytec.Protocol;

public static partial class SCL
{
    public class Pack : IPackage, ICloneable<Pack>, IEquatable<Pack?>
    {
        public const ushort NoPassword = 0;
        public const ushort PasswordAccepted = 0xffff;

        private static ushort NextPackIndex = (ushort)new Random().Next();
        private static readonly object SyncRoot = new object();
        public static Func<ushort> GetNextPackIndex { get; set; } = () =>
        {
            ushort id;
            lock (SyncRoot)
                id = ++NextPackIndex;
            return id;
        };

        public IdentifierType Identifier
        {
            get
            {
                var id = IdentifierType.Default;
                if (IsSCL)
                    id |= IdentifierType.IsSCL;
                if (IsNet)
                    id |= IdentifierType.IsNet;
                if (IsAnswer)
                    id |= IdentifierType.IsRcv;
                return id;
            }
            set
            {
                IsSCL = value.HasFlags(IdentifierType.IsSCL);
                IsNet = value.HasFlags(IdentifierType.IsNet);
                IsAnswer = value.HasFlags(IdentifierType.IsRcv);
            }
        }
        public bool IsSCL { get; set; }
        public bool IsAnswer { get; set; }
        public bool IsNet { get; set; }
        public byte AddrCode { get; set; }
        public ushort Password { get; set; } = NoPassword;
        public uint Index { get; set; }

        public CommandCode Command
        {
            get => (CommandCode)CommandCode;
            set => CommandCode = (int)value;
        }
        public int CommandCode { get; set; }
        public int Arg1 { get; set; }
        public int Arg2 { get; set; }
        public byte[] Arg3 { get; set; } = Array.Empty<byte>();


        public Encoding Encoding { get; set; } = DefaultEncode;

        public Pack() { }
        public Pack(Pack other)
        {
            Identifier = other.Identifier;
            AddrCode = other.AddrCode;
            Password = other.Password;
            Index = other.Index;
            CommandCode = other.CommandCode;
            Arg1 = other.Arg1;
            Arg2 = other.Arg2;
            Arg3 = other.Arg3.ToArray();
            Encoding = other.Encoding;
        }

        public void UpdatePackIndex() => Index = GetNextPackIndex();

        public bool IsMyAnswer(Pack p)
        => !IsAnswer && p.IsAnswer
            && IsNet == p.IsNet
            && IsSCL == p.IsSCL
            && AddrCode == p.AddrCode
            && Index == p.Index
            && CommandCode == CommandCode;

        public bool IsPasswordAccepted => IsAnswer && (Password == NoPassword || Password == PasswordAccepted);

        public static byte[] ConvertIdentifier(string id) => Encoding.ASCII.GetBytes(id);

        public const string NetSuperCommSend = "LYTC";
        public const string NetSuperCommRecv = "LyTc";
        public const string NetSCL2008Send = "TCLY";
        public const string NetSCL2008Recv = "tClY";
        public static readonly byte[] NetSCSend = ConvertIdentifier(NetSuperCommSend);
        public static readonly byte[] NetSCRecv = ConvertIdentifier(NetSuperCommRecv);
        public static readonly byte[] NetSCLSend = ConvertIdentifier(NetSCL2008Send);
        public static readonly byte[] NetSCLRecv = ConvertIdentifier(NetSCL2008Recv);

        public const string UartSuperCommSend = "\x1b&LyTec";
        public const string UartSuperCommRecv = "\x1b&lYtEc";
        public const string UartSCL2008Send = "\x1b&LyTeC";
        public const string UartSCL2008Recv = "\x1b&lYtEC";
        public static readonly byte[] UartSCSend = ConvertIdentifier(UartSuperCommSend);
        public static readonly byte[] UartSCRecv = ConvertIdentifier(UartSuperCommRecv);
        public static readonly byte[] UartSCLSend = ConvertIdentifier(UartSCL2008Send);
        public static readonly byte[] UartSCLRecv = ConvertIdentifier(UartSCL2008Recv);

        [Flags]
        public enum IdentifierType
        {
            Default = 0,
            IsSCL = 1 << 0,
            IsNet = 1 << 1,
            IsRcv = 1 << 2,
            NetSCSend   = IsNet,
            NetSCRecv   = IsNet | IsRcv,
            NetSCLSend  = IsNet | IsSCL,
            NetSCLRecv  = IsNet | IsSCL | IsRcv,
            UartSCSend  = 0,
            UartSCRecv  = IsRcv,
            UartSCLSend = IsSCL,
            UartSCLRecv = IsSCL | IsRcv,
        }

        public static IReadOnlyDictionary<IdentifierType, byte[]> IdentifierBytes = new (IdentifierType, byte[])[]
        {
            ( IdentifierType.NetSCSend   , NetSCSend   ),
            ( IdentifierType.NetSCRecv   , NetSCRecv   ),
            ( IdentifierType.NetSCLSend  , NetSCLSend  ),
            ( IdentifierType.NetSCLRecv  , NetSCLRecv  ),
            ( IdentifierType.UartSCSend  , UartSCSend  ),
            ( IdentifierType.UartSCRecv  , UartSCRecv  ),
            ( IdentifierType.UartSCLSend , UartSCLSend ),
            ( IdentifierType.UartSCLRecv , UartSCLRecv ),
        }.ToDictionary(kv => kv.Item1, kv => kv.Item2);

        public void SetPassword(string pw) => Password = CreateCRC16().Compute(Encoding.GetBytes(pw));

        public byte[] Serialize()
        {
            var buf = new List<byte>();
            var id = IdentifierType.Default;
            if (IsNet)
                id |= IdentifierType.IsNet;
            if (IsSCL)
                id |= IdentifierType.IsSCL;
            if (IsAnswer)
                id |= IdentifierType.IsRcv;
            buf.AddRange(IdentifierBytes[id]);
            if (!IsNet)
                buf.Add(AddrCode);
            buf.AddRange(Index.ToBytes(DefaultEndian));
            var crcbuf = new List<byte>();
            var crcpos = -1;
            if (IsNet)
            {
                ushort len = (ushort)(24 + Arg3.Length);
                buf.AddRange(len.ToBytes(DefaultEndian));
                buf.AddRange(Password.ToBytes(DefaultEndian));
            }
            else
            {
                crcpos = buf.Count;
                // CRC占位
                buf.Add(0); buf.Add(0);
                ushort len = (ushort)(28 + Arg3.Length);
                var lenbuf = len.ToBytes(DefaultEndian);
                crcbuf.AddRange(lenbuf);
                buf.AddRange(lenbuf);
            }
            {
                var buf2 = CommandCode.ToBytes(DefaultEndian);
                crcbuf.AddRange(buf2);
                buf.AddRange(buf2);
            }
            {
                var buf2 = Arg1.ToBytes(DefaultEndian);
                crcbuf.AddRange(buf2);
                buf.AddRange(buf2);
            }
            {
                var buf2 = Arg2.ToBytes(DefaultEndian);
                crcbuf.AddRange(buf2);
                buf.AddRange(buf2);
            }
            if (Arg3.Length > 0)
            {
                crcbuf.AddRange(Arg3);
                buf.AddRange(Arg3);
            }
            if (crcpos > -1 && (crcpos + 2) <= buf.Count)
            {
                ushort crc = CreateCRC16().Compute(crcbuf);
                var buf2 = crc.ToBytes(DefaultEndian);
                for (var i = 0; i < buf2.Length; i++)
                    buf[crcpos + i] = buf2[i];
            }
            return buf.ToArray();
        }

        public class Deserializer
        {
            public static Deserializer Default { get; set; } = new();

            public static Func<ITimer>? CreateAutoResetTimer { get; set; }

            public virtual ITimer? RecvTimeoutTimer { get; set; }

            public virtual int RecvTimeout { get; set; } = 500;

            protected virtual IList<byte> Cache { get; set; }
            protected virtual int Step { get; set; }
            protected virtual int StepLen { get; set; }

            public IReadOnlyList<KeyValuePair<IdentifierType, byte[]>> AllIdentifiers { get; set; } = IdentifierBytes.ToArray();
            protected virtual IList<KeyValuePair<IdentifierType, byte[]>> Identifiers { get; set; } = IdentifierBytes.ToArray();

            protected virtual Pack Pack { get; set; } = new();

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
                Identifiers = AllIdentifiers.ToList();
                RecvTimeoutTimer?.Stop();
                Cache.Clear();
                Pack = new();
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
            }

            static readonly IReadOnlyList<Steps> NetSteps = new Steps[]
            {
                Steps.Identifier,
                Steps.PackIndex,
                Steps.DataLen,
                Steps.Password,
                Steps.Data,
            };

            static readonly IReadOnlyList<Steps> UartSteps = new Steps[]
            {
                Steps.Identifier,
                Steps.AddrCode,
                Steps.PackIndex,
                Steps.CheckSum,
                Steps.DataLen,
                Steps.Data,
            };

            static IReadOnlyDictionary<Steps, int> GetNoValidStepLens(IEnumerable<Steps> steps)
            => steps.Select(d =>
            {
                var len = d switch
                {
                    Steps.Identifier => 0,
                    Steps.AddrCode => 1,
                    Steps.PackIndex => 4,
                    Steps.Password => 2,
                    Steps.DataLen => 2,
                    Steps.Data => 0,
                    Steps.CheckSum => 2,
                    _ => 0,
                };
                return (d, len);
            }).Where(v => v.len > 0).ToDictionary(x => x.d, x => x.len);

            static readonly IReadOnlyDictionary<Steps, int> NoValidNetStepLens = GetNoValidStepLens(NetSteps);
            static readonly IReadOnlyDictionary<Steps, int> NoValidUartStepLens = GetNoValidStepLens(UartSteps);

            private ushort CRC16 = 0;
            private int DataLen = 0;
            public virtual Pack? Deserialize(byte d)
            {
                var rlen = Cache.Count;
                var beforerlen = rlen;
                var step = (Steps)Step;
                var moveStep = false;
                if (step != Steps.Identifier && (Pack.IsNet ? NoValidNetStepLens : NoValidUartStepLens).TryGetValue(step, out var len))
                {
                    Cache.Add(d);
                    rlen++;
                    StepLen++;
                    if (StepLen >= len)
                    {
                        switch (step)
                        {
                            case Steps.AddrCode:
                                Pack.AddrCode = Cache.Last();
                                break;
                            case Steps.PackIndex:
                                Pack.Index = Cache.Skip(Cache.Count - 4).ToArray().ToStruct<uint>(DefaultEndian);
                                break;
                            case Steps.Password:
                                Pack.Password = Cache.Skip(Cache.Count - 2).ToArray().ToStruct<ushort>(DefaultEndian);
                                break;
                            case Steps.DataLen:
                                DataLen = Cache.Skip(Cache.Count - 2).ToArray().ToStruct<ushort>(DefaultEndian);
                                if (Pack.IsNet)
                                    DataLen -= 12; // 减掉外层封装长度
                                if (DataLen < 12)
                                {
                                    Reset();
                                    return null;
                                }
                                break;
                            case Steps.CheckSum:
                                CRC16 = Cache.Skip(Cache.Count - 2).ToArray().ToStruct<ushort>(DefaultEndian);
                                break;
                        }
                        moveStep = true;
                    }
                }
                else
                {
                    switch (step)
                    {
                        case Steps.Identifier:
                            if (rlen == 0)
                            {
                                if (Identifiers.Any(x => x.Value[0] == d))
                                {
                                    RecvTimeoutTimer?.Start();
                                    Cache.Add(d);
                                    rlen++;
                                    StepLen++;
                                }
                            }
                            else
                            {
                                Cache.Add(d);
                                rlen++;
                                StepLen++;
                                if (Identifiers.Count > 1)
                                {
                                    for (int i = Identifiers.Count - 1; i >= 0; i--)
                                    {
                                        if (Identifiers[i].Value.Length < Cache.Count || !Identifiers[i].Value.Take(Cache.Count).SequenceEqual(Cache))
                                            Identifiers.RemoveAt(i);
                                    }
                                }
                                else if (Identifiers[0].Value[Cache.Count - 1] != d)
                                    Identifiers.Clear();
                                if (Identifiers.Count > 0)
                                {
                                    for (var i = 0; i < Identifiers.Count; i++)
                                    {
                                        if (Identifiers[i].Value.Length == Cache.Count)
                                        {
                                            Pack.Identifier = Identifiers[i].Key;
                                            moveStep = true;
                                            break;
                                        }
                                    }
                                }
                                else
                                {
                                    // 丢弃无效数据
                                    Reset();
                                }
                            }
                            break;
                        case Steps.Data:
                            Cache.Add(d);
                            rlen++;
                            StepLen++;
                            if (StepLen >= DataLen)
                            {
                                var offset = Cache.Count - DataLen;
                                Pack.CommandCode = Cache.Skip(offset).Take(4).ToArray().ToStruct<int>(DefaultEndian);
                                offset += 4;
                                Pack.Arg1 = Cache.Skip(offset).Take(4).ToArray().ToStruct<int>(DefaultEndian);
                                offset += 4;
                                Pack.Arg2 = Cache.Skip(offset).Take(4).ToArray().ToStruct<int>(DefaultEndian);
                                offset += 4;
                                if (offset < Cache.Count)
                                    Pack.Arg3 = Cache.Skip(offset).ToArray();
                                else Pack.Arg3 = Array.Empty<byte>();
                                moveStep = true;
                            }
                            break;
                    }
                }
                if (moveStep)
                {
                    var steps = Pack.IsNet ? NetSteps : UartSteps;
                    var stepi = steps.IndexOf(step);
                    if (stepi == -1 || stepi == (steps.Count - 1))
                    {
                        RecvTimeoutTimer?.Stop();
                        moveStep = false;
                        if (stepi != -1)
                        {
                            // 收完了所有数据
                            if (Pack.IsNet || CRC16 == CreateCRC16().Compute(Cache.Skip(14)))
                                return Pack.Clone();
                        }
                        Reset();
                    }
                    else MoveStep((int)steps[stepi + 1]);
                }
                if (moveStep || rlen > beforerlen)
                    RecvTimeoutTimer?.Restart();
                return null;
            }

            public virtual Pack? Deserialize(IEnumerable<byte> d, out int DeserializedLength)
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

            public virtual Pack? Deserialize(IEnumerable<byte> b)
            => Deserialize(b, out _);
        }

        public static Func<Deserializer> CreateDeserializer { get; set; } = () => new Deserializer();

        public static Pack? Deserialize(IEnumerable<byte> data, out int DeserializedLength) => Deserializer.Default.Deserialize(data, out DeserializedLength);

        public static Pack? Deserialize(byte[] bytes, int offset = 0) => Deserialize(bytes.Skip(offset), out _);

        public Pack Clone() => new(this);

        object ICloneable.Clone() => Clone();

        public override bool Equals(object? obj) => Equals(obj as Pack);

        public bool Equals(Pack? other)
        => other is not null &&
            Identifier == other.Identifier &&
            AddrCode == other.AddrCode &&
            Password == other.Password &&
            Index == other.Index &&
            CommandCode == other.CommandCode &&
            Arg1 == other.Arg1 &&
            Arg2 == other.Arg2 &&
            EqualityComparer<byte[]>.Default.Equals(Arg3, other.Arg3);

        public override int GetHashCode()
        {
            int hashCode = -1121651732;
            hashCode = hashCode * -1521134295 + Identifier.GetHashCode();
            hashCode = hashCode * -1521134295 + AddrCode.GetHashCode();
            hashCode = hashCode * -1521134295 + Password.GetHashCode();
            hashCode = hashCode * -1521134295 + Index.GetHashCode();
            hashCode = hashCode * -1521134295 + CommandCode.GetHashCode();
            hashCode = hashCode * -1521134295 + Arg1.GetHashCode();
            hashCode = hashCode * -1521134295 + Arg2.GetHashCode();
            hashCode = hashCode * -1521134295 + EqualityComparer<byte[]>.Default.GetHashCode(Arg3);
            return hashCode;
        }

        public static bool operator ==(Pack? left, Pack? right)
        {
            if (left is not null)
                return left.Equals(right);
            else if (right is not null)
                return right.Equals(left);
            else return true;
        }

        public static bool operator !=(Pack? left, Pack? right) => !(left == right);
    }
}
