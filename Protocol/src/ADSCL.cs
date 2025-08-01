using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using Lytec.Common.Communication;
using Lytec.Common.Data;
using Lytec.Common.Serialization;

namespace Lytec.Protocol
{
    public class ADSCL
    {
        public const Endian DefaultEndian = Endian.Little;
        public const CharSet DefaultCharSet = CharSet.Ansi;
        public static Encoding DefaultEncode { get; set; } = Encoding.GetEncoding(936);


        [Serializable]
        [BigEndian]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [DebuggerDisplay("{" + nameof(Major) + "}.{" + nameof(Minor) + "}.{" + nameof(Build) + "}")]
        public readonly struct VersionCode32BigEndian : IPackage, IVersionData
        {
            public byte Major { get; }
            public byte Minor { get; }
            public ushort Build { get; }

            int IVersionData.Major => Major;
            int IVersionData.Minor => Minor;
            int IVersionData.Build => Build;

            public VersionCode32BigEndian(int major, int minor, int build = 0) : this() => (Major, Minor, Build) = ((byte)major, (byte)minor, (ushort)build);

            public override string ToString() => $"{Major}.{Minor}.{Build}";

            public byte[] Serialize() => this.ToBytes();
            public static VersionCode32BigEndian Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<VersionCode32BigEndian>(offset);

            public static explicit operator int(VersionCode32BigEndian v) => v.Serialize().ToStruct<int>();
            public static explicit operator VersionCode32BigEndian(int v) => Deserialize(v.ToBytes());
        }

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
                var dbuf = Data.Serialize();
                var dlen = (ushort)dbuf.Length;
                buf.Add(dlen.ToBytes(DefaultEndian));
                buf.Add(dbuf);
                buf.Add(CheckSum.ToBytes(Endian.Big));
#pragma warning restore IDE0028 // 简化集合初始化
                return buf.ToArray();
            }

            public class Deserializer : IDeserializer<TImpl>
            {
                public static readonly Deserializer Default = new Deserializer();

                public static Func<ITimer> CreateAutoResetTimer { get; set; }

                public virtual ITimer RecvTimeoutTimer { get; set; }

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

                public virtual void Reset()
                {
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
                public virtual TImpl Deserialize(byte d)
                {
                    void MoveStep(int step)
                    {
                        StepLen = 0;
                        Step = step;
                    }

                    var RcvLen = Cache.Count;
                    var rcvLen = RcvLen;
                    if (NoValidStepLens.TryGetValue((Steps)Step, out var len))
                    {
                        Cache.Add(d);
                        StepLen++;
                        if (StepLen >= len)
                        {
                            switch ((Steps)Step)
                            {
                                case Steps.DataLen:
                                    DataLen = Cache.Skip(Cache.Count - 2).ToArray().ToStruct<ushort>();
                                    if (DataLen < MinDataLength)
                                    {
                                        Reset();
                                        return null;
                                    }
                                    break;
                                case Steps.CheckSum:
                                    if (GetCRC16Algorithm().ComputeHash(Cache) != 0)
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
                    TImpl p = null;
                    switch ((Steps)Step)
                    {
                        case Steps.Fin:
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
                            RecvTimeoutTimer?.Stop();
                            break;
                        default:
                            if (rcvLen != RcvLen)
                                RecvTimeoutTimer?.Restart();
                            break;

                    }
                    return p;
                }
                public virtual TImpl Deserialize(IEnumerable<byte> d, out int DeserializedLength)
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

                public virtual TImpl Deserialize(IEnumerable<byte> b)
                => Deserialize(b, out _);
            }

            public static Func<Deserializer> CreateDeserializer { get; set; } = () => new Deserializer();

            public static TImpl Deserialize(byte d) => Deserializer.Default.Deserialize(d);
            public static TImpl Deserialize(IEnumerable<byte> d, out int DeserializedLength) => CreateDeserializer().Deserialize(d, out DeserializedLength);

            IDeserializer<TImpl> IFactory<IDeserializer<TImpl>>.Create() => CreateDeserializer?.Invoke();
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
            public virtual byte[] Arg3 { get; set; }

            public CommandPack() { }
            public CommandPack(int command, int arg1, int arg2, byte[] arg3 = null)
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
                public CommandPack Deserialize(IEnumerable<byte> b)
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

            public CommandPack Deserialize(byte[] bytes) => CreateDeserializer().Deserialize(bytes);

            public virtual CommandPack Clone() => new CommandPack(Command, Arg1, Arg2, Arg3?.ToArray());

            object ICloneable.Clone() => Clone();
        }

        [Serializable]
#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
        public enum DiskDriver : byte
        {
            A = 0,
            B = 1,
            C = 2
        }

        [Serializable]
#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
        public enum SpStructIndex
        {
            RuntimeInfo = 0,
            AllConfigs = 1,
            FPGARAM_1stPart = 2,
            FPGARAM_2ndPart = 3,
        }

        public static bool Exec(SendAndGetAnswerConfig conf, [System.Diagnostics.CodeAnalysis.NotNullWhen(true)] out Pack Answer, CommandPack command, string password = null, Func<Pack, bool> CheckIsSuccess = default, int extTimeout = 0)
        {
            var cmd = new Pack()
            {
                AddrCode = (byte)(conf.AddrCode != DontUseAddrCode ? conf.AddrCode : 0),
                Data = command.Clone(),
                Identifier = Pack.SendIdentifier.ToArray(),
            };
            if (!password.IsNullOrEmpty())
                cmd.Password = Pack.PasswordConverter.Convert(password);
            cmd.UpdatePackIndex();
            cmd.UpdateCheckSum();
            var sbuf = cmd.Serialize();
            var deserializer = Pack.CreateDeserializer();
            Answer = null;
            for (var tryCount = -1; tryCount < conf.Retries; tryCount++)
            {
                deserializer.Reset();
                byte[] recv = null;
                try
                {
                    if (!conf.SendAndGetAnswer(sbuf, out recv, extTimeout))
                        continue;
                }
                catch (TimeoutException)
                {
                    continue;
                }
                var answer = deserializer.Deserialize(recv);
                if (answer == null || !cmd.IsMyAnswer(answer))
                    continue;
                Answer = answer;
                if (CheckIsSuccess == null)
                    CheckIsSuccess = p => p.Data.Arg2 != SCL.FalseValue;
                return Answer != null && CheckIsSuccess(Answer);
            }
            return false;
        }

        public static bool LoadFrom(SendAndGetAnswerConfig config, int addr, int length, out byte[] Data, Func<Pack, bool> CheckIsValidData = null, string password = null)
        {
            Data = null;
            if (!Exec(
                    config,
                    out var ans,
                    new CommandPack((int)SCL.Command.LoadFrom, addr, length),
                    password,
                    r => r.Data.Arg2 != SCL.FalseValue && (CheckIsValidData == null || CheckIsValidData(r))
                    ))
                return false;
            Data = ans.Data.Arg3;
            return true;
        }

        [Serializable]
#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
        public enum CardType : byte
        {
            SCL2008 = 0xff,
            ADSCL2500 = 0,
            ADSCL2800 = 1,
            CHECK2500 = 2,
            ADSCL2900 = 3,
            CHECK2600 = 4,
        }

        [Serializable]
#if USE_NEWTONSOFT_JSON
        [JsonConverter(typeof(StringEnumConverter))]
#endif
        public enum TestPlayType : byte
        {
            Default = 0,
            None = 0x55,
            FourHighlightDots = 0xAA,
            DotByDot = 0x3C
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct AnalogIOConfig
        {
            public int Data;

            public int Mode
            {
                get => BitHelper.GetValue(Data, 0, 4);
                set => Data = BitHelper.SetValue(Data, value, 0, 4);
            }
        }

        public interface IModuleConfigs : IPackage
        {
            byte Chip { get; set; }
        }

        public enum Module2500Chip : byte
        {
            Normal = 0,
            SM16188 = 1,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2500Configs : IModuleConfigs
        {
            public Module2500Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2500Chip)value; }

            public byte OptionBits { get; set; }

            public bool UseFpgaUart
            {
                get => BitHelper.GetFlag(OptionBits, 0);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 0);
            }

            public bool FpgaUartOutputIsSerial
            {
                get => BitHelper.GetFlag(OptionBits, 1);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 1);
            }

            public bool FpgaUartInputIsSerial
            {
                get => BitHelper.GetFlag(OptionBits, 2);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 2);
            }

            public bool ScreenCopyToFpgaUart
            {
                get => BitHelper.GetFlag(OptionBits, 3);
                set => OptionBits = (byte)BitHelper.SetFlag(OptionBits, value, 3);
            }

            public int FpgaUartBaudrate { get; set; }

            public byte[] Serialize() => this.ToBytes();
        }
        
        public enum Module2800Chip : byte
        {
            Normal = 0,
            LYAN6039 = Normal,
            SM16389SF = Normal,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2800Configs : IModuleConfigs
        {
            public Module2800Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2800Chip)value; }

            public byte[] Serialize() => this.ToBytes();

        }

        public enum Module2900Chip : byte
        {
            Normal = 0,
            ICND2110 = Normal,
            ICND2112 = 1,
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public struct Module2900Configs : IModuleConfigs
        {
            public Module2900Chip Chip { get; set; }

            byte IModuleConfigs.Chip { get => (byte)Chip; set => Chip = (Module2900Chip)value; }

            public byte[] Serialize() => this.ToBytes();
        }

        [Serializable]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Endian(DefaultEndian)]
        public class LEDConfig : IPackage
        {
            public const int SizeConst = 256;
            static LEDConfig() => Debug.Assert(Marshal.SizeOf<LEDConfig>() == SizeConst);

            public int OptionBits { get; set; }

            public int ColExchanges
            {
                get => BitHelper.GetValue(OptionBits, 0, 4);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 0, 4);
            }

            public int RowExchanges
            {
                get => BitHelper.GetValue(OptionBits, 4, 4);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 4, 4);
            }

            public bool ReverseEvenRowCols
            {
                get => BitHelper.GetFlag(OptionBits, 8);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 8);
            }

            public bool LineOrderOffsetDec1
            {
                get => BitHelper.GetFlag(OptionBits, 9);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 9);
            }

            public bool IsOnLeftSide
            {
                get => BitHelper.GetFlag(OptionBits, 10);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 10);
            }

            public bool InvertDataSignal
            {
                get => BitHelper.GetFlag(OptionBits, 11);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 11);
            }

            public bool LineDecode
            {
                get => BitHelper.GetFlag(OptionBits, 12);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 12);
            }

            public bool WidenOESignal
            {
                get => BitHelper.GetFlag(OptionBits, 13);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 13);
            }

            public bool IsFullColor
            {
                get => BitHelper.GetFlag(OptionBits, 14);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 14);
            }

            public bool IsCompactColorSignals
            {
                get => BitHelper.GetFlag(OptionBits, 15);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 15);
            }

            public SCL.ClockDuty ClockDuty
            {
                get => (SCL.ClockDuty)BitHelper.GetValue(OptionBits, 16, 2);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 16, 2);
            }

            public SCL.ControlRange Scale
            {
                get => (SCL.ControlRange)BitHelper.GetValue(OptionBits, 18, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 18, 3);
            }

            public SCL.ControlRange Range
            {
                get => (SCL.ControlRange)BitHelper.GetValue(OptionBits, 21, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 21, 3);
            }

            public SCL.ColorOrder ColorOrder
            {
                get => (SCL.ColorOrder)BitHelper.GetValue(OptionBits, 24, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, (int)value, 24, 3);
            }

            public bool Rotate
            {
                get => BitHelper.GetFlag(OptionBits, 27);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 27);
            }

            public bool UseCheckCard
            {
                get => BitHelper.GetFlag(OptionBits, 28);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 28);
            }

            public bool DotCheckIgnoreR
            {
                get => BitHelper.GetFlag(OptionBits, 29);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 29);
            }

            public bool DotCheckIgnoreG
            {
                get => BitHelper.GetFlag(OptionBits, 30);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 30);
            }

            public bool DotCheckIgnoreB
            {
                get => BitHelper.GetFlag(OptionBits, 31);
                set => OptionBits = BitHelper.SetFlag(OptionBits, value, 31);
            }

            public int DotCheckColorMask
            {
                get => BitHelper.GetValue(OptionBits, 29, 3);
                set => OptionBits = BitHelper.SetValue(OptionBits, value, 29, 3);
            }

            public byte ScanMode { get; set; }

            public byte DataGroupHeight { get; set; }

            public byte ModuleWidth { get; set; }

            public byte ModuleHeight { get; set; }

            public ushort LedWidth { get; set; }

            public ushort LedHeight { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 5)]
            public byte[] VINTFreqDiv { get; set; } = new byte[5];

            public CardType CardType { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] ModuleConfigsData { get; set; } = new byte[16];
            public IModuleConfigs ModuleConfigs
            {
                get
                {
                    switch (CardType)
                    {
                        case CardType.ADSCL2500:
                            return ModuleConfigsData.ToStruct<Module2500Configs>();
                        case CardType.ADSCL2800:
                            return ModuleConfigsData.ToStruct<Module2800Configs>();
                        case CardType.ADSCL2900:
                            return ModuleConfigsData.ToStruct<Module2900Configs>();
                        default:
                            return null;
                    }
                }
                set => ModuleConfigsData = value.Serialize().Concat(Enumerable.Repeat<byte>(0, 16)).Take(16).ToArray();
            }

            public byte GammaValue { get; set; }
            public float Gamma { get => GammaValue / 10f; set => GammaValue = (byte)(value * 10); }

            public byte BrightOptions { get; set; }

            [Range(0, 63)]
            public int Bright
            {
                get => BitHelper.GetValue(BrightOptions, 0, 6);
                set => BrightOptions = (byte)BitHelper.SetValue(BrightOptions, Utils.LimitToRange(0, value, 63), 0, 6);
            }

            public bool UseAutoBright
            {
                get => BitHelper.GetFlag(BrightOptions, 6);
                set => BrightOptions = (byte)BitHelper.SetFlag(BrightOptions, value, 6);
            }

            [Range(0, 63)]
            public byte AutoBrightMaxLevel { get; set; }

            [Range(0, 63)]
            public byte AutoBrightMinLevel { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] BrightSensorADVTable { get; set; } = new byte[32];

            public ushort PowerOnTimeValue { get; set; }
            public (int Hour, int Minute) PowerOnTime
            {
                get => (PowerOnTimeValue >> 8, PowerOnTimeValue & 0xff);
                set => PowerOnTimeValue = (ushort)((Utils.LimitToRange(0, value.Hour, 23) << 8) | Utils.LimitToRange(0, value.Minute, 59));
            }

            public ushort PowerOffTimeValue { get; set; }
            public (int Hour, int Minute) PowerOffTime
            {
                get => (PowerOffTimeValue >> 8, PowerOffTimeValue & 0xff);
                set => PowerOffTimeValue = (ushort)((Utils.LimitToRange(0, value.Hour, 23) << 8) | Utils.LimitToRange(0, value.Minute, 59));
            }

            public short TemperatureOffset { get; set; }

            public TestPlayType TestPlayType { get; set; }

            public ushort SPIHideSecCount { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
            public SCL.UartConfig[] ComPara { get; set; }

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 6)]
            public AnalogIOConfig[] AnalogIOConfigs { get; set; } = new AnalogIOConfig[6];

            public byte StaggerOptions { get; set; }

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 114)]
            private readonly byte[] unused = new byte[114];

            public uint PoTimeout { get; set; }

            public ushort Valid { get; set; }

            public bool IsValid => Valid == 0xAA55;

            public byte[] Serialize() => this.ToBytes();

            public static LEDConfig TryParse(byte[] bytes, int offset = 0) => bytes.ToStruct<LEDConfig>(offset);
        }

        [Serializable]
        [Endian(SCL.DefaultEndian)]
        public readonly struct AllConfigs
        {
            public const int SizeConst = SCL.MacConfig.SizeConst + SCL.NetConfig.SizeConst + LEDConfig.SizeConst;
            static AllConfigs() => Debug.Assert(Marshal.SizeOf<AllConfigs>() == SizeConst);
            public SCL.MacConfig Mac { get; }
            public SCL.NetConfig Net { get; }
            public LEDConfig Led { get; }
        }

        public static bool LoadFrom(SendAndGetAnswerConfig config, int addr, int length, out byte[] Data, int minDataLen, string password = null)
        => LoadFrom(config, addr, length, out Data, r => r.Data.Arg2 >= minDataLen && r.Data.Arg3?.Length >= minDataLen, password);

        public static bool GetSpStructInternal(SendAndGetAnswerConfig config, SpStructIndex index, out byte[] data, int minDataLen, string password = null)
        => LoadFrom(config, (int)SCL.StructAddress.LoadFromSpStructs, (int)index, out data, minDataLen, password);

        //public static bool GetVersionCode(SendAndGetAnswerConfig config, out SCL.VersionCode Version, string password = null)
        //=> GetSpStruct(config, SCL.SpStructIndex.FullVersionCode, out Version, SCL.VersionCode.SizeConst, password);

        public static bool GetSpStruct<T>(SendAndGetAnswerConfig config, SpStructIndex index, out T data, int minDataLen, string password = null)
        {
            var ret = GetSpStructInternal(config, index, out var bytes, minDataLen, password);
            data = ret ? bytes.ToStruct<T>(0, SCL.DefaultEndian) : default;
            return ret;
        }

        public static bool GetAllConfigs(SendAndGetAnswerConfig config, out AllConfigs Configs, string password = null)
        => GetSpStruct(config, SpStructIndex.AllConfigs, out Configs, AllConfigs.SizeConst, password);

        public static bool GetMacConfig(SendAndGetAnswerConfig config, out SCL.MacConfig Config, string password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Mac : default;
            return ret;
        }
        
        public static bool GetNetConfig(SendAndGetAnswerConfig config, out SCL.NetConfig Config, string password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Net : default;
            return ret;
        }
        
        public static bool GetLedConfig(SendAndGetAnswerConfig config, out LEDConfig Config, string password = null)
        {
            var ret = GetAllConfigs(config, out var cfgs, password);
            Config = ret ? cfgs.Led : default;
            return ret;
        }

        public static bool SendData(SendAndGetAnswerConfig config, int addr, IEnumerable<byte> data, string password = null, int extTimeout = 0)
        {
            while (data.Any())
            {
                var buf = data.Take(SCL.MaxDataLength).ToArray();
                data = data.Skip(SCL.MaxDataLength);
                if (!Exec(config, out _, new CommandPack((int)SCL.Command.SendData, addr, buf.Length, buf), password, r => r.Data.Arg2 == buf.Length, extTimeout))
                    return false;
            }
            return true;
        }

        public static bool SaveTo(SendAndGetAnswerConfig config, int addr, int length, string password = null, int extTimeout = 0)
        => Exec(config, out _, new CommandPack((int)SCL.Command.SaveTo, addr, length), password, r => r.Data.Arg2 == length, extTimeout);

        public static bool SetLEDConfig(SendAndGetAnswerConfig config, LEDConfig conf, string password = null)
        => SendData(config, 0, conf.ToBytes(), password)
            && SaveTo(config, (int)SCL.StructAddress.LEDConfig, LEDConfig.SizeConst, password);

        /// <summary>
        /// 格式化磁盘，仅支持内置存储（A盘）和RAM内存盘（C盘）
        /// </summary>
        /// <param name="config">通信配置</param>
        /// <param name="drv">目标磁盘</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool FormatDisk(SendAndGetAnswerConfig config, DiskDriver drv, string password = null)
        => Exec(config, out _, new CommandPack((int)SCL.Command.FormatDisk, (int)drv, 0), password);

        /// <summary>
        /// 重新播放节目表
        /// </summary>
        /// <param name="config"></param>
        /// <param name="driver">节目表所在磁盘</param>
        /// <param name="index">节目表索引</param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Replay(SendAndGetAnswerConfig config, DiskDriver driver, int index, string password = null)
        => Exec(config, out _, new CommandPack((int)SCL.Command.Reset, 0, ((index & 0xff) << 24) | ((int)driver << 16)), password);

        /// <summary>
        /// 重启设备
        /// </summary>
        /// <param name="config"></param>
        /// <param name="password">网络通信密码</param>
        /// <returns></returns>
        public static bool Reboot(SendAndGetAnswerConfig config, string password = null)
        => Exec(config, out _, new CommandPack((int)SCL.Command.Reset, 1, 0), password);

    }
}
