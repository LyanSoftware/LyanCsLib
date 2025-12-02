using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
    [Endian(DefaultEndian)]
    public class HeartbeatPack
    {
        public const int SizeConst = 32;

        static HeartbeatPack() => Debug.Assert(Marshal.SizeOf<HeartbeatPack>() == SizeConst);


        public readonly MacAddress MAC;
        public readonly IPv4Address IP;
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = NameSize)]
        private readonly byte[] NameBytes;
        public string Name => GetStringFromFixedLength(NameBytes);
        public readonly SCLType Type;
        public readonly byte TimeDay;
        public readonly byte TimeHour;
        public readonly byte TimeMinute;
        public readonly ushort CRC;
        private HeartbeatPack() => NameBytes = new byte[NameSize];
        public static HeartbeatPack? Deserialize(byte[] bytes)
        {
            if (bytes.Length != SizeConst)
                return null;
            var crc = CreateCRC16().Compute(bytes, 0, bytes.Length - 2);
            var pack = bytes.ToStruct<HeartbeatPack>();
            if (pack.CRC != crc)
                return null;
            return pack;
        }
    }

    public static HeartbeatPack? CheckHeartbeat(byte[] data) => HeartbeatPack.Deserialize(data);

}
