using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1, CharSet = CharSet.Ansi)]
    [Endian(DefaultEndian)]
    public struct SpConfig : IPackage
    {
        public const int SizeConst = 256;

        static SpConfig() => Debug.Assert(Marshal.SizeOf<SpConfig>() == SizeConst);


        public const ushort FooterIdentifier = 0xAA55;

        public byte PCBVer { get; set; }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 0xEF)]
        private readonly byte[] unused;

        public string DataOK
        {
            get => GetStringFromFixedLength(_DataOK);
            set => _DataOK = GetFixedLengthStringWithFlash(value, 14);
        }
        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 14)]
        private byte[] _DataOK;

        public ushort FooterID { get; set; }

        public bool IsValid => FooterID == FooterIdentifier;

        public static SpConfig CreateInstance() => InnerDeserialize(InitFlashDataBlock(SizeConst));

        public byte[] Serialize() => this.ToBytes();

        public static SpConfig InnerDeserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<SpConfig>(offset);

        public static SpConfig Deserialize(byte[] bytes, int offset = 0)
        {
            var info = InnerDeserialize(bytes, offset);
            return info.IsValid ? info : throw new ArgumentException();
        }
    }
}
