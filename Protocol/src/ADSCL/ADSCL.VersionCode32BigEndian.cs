using System.Diagnostics;
using System.Runtime.InteropServices;
using Lytec.Common.Communication;
using Lytec.Common.Data;

namespace Lytec.Protocol
{
    public partial class ADSCL
    {
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

    }
}
