using Lytec.Common.Data;
using Lytec.Common.Communication;
using System.Runtime.InteropServices;
using System.Diagnostics;
using System.Text;

namespace Lytec.Protocol.Ly;

[Serializable]
[StructLayout(LayoutKind.Sequential, Pack = 1)]
[Endian(Endian.Big)]
public readonly struct VersionInfo : IPackage
{
    public const int SizeConst = 18;
    static VersionInfo() => Debug.Assert(Marshal.SizeOf<VersionInfo>() == SizeConst);

    public uint ProgramSize { get; }
    [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
    public byte[] IdentifierBytes { get; }
    public string Identifier => Encoding.ASCII.GetString(IdentifierBytes);
    public ushort VersionID { get; }
    public byte MajorVer { get; }
    public byte MinorVer { get; }
    public ushort BuildVer { get; }

    public byte[] Serialize() => this.ToBytes();
    public static VersionInfo? Deserialize(byte[] data, int offset = 0)
    {
        try
        {
            return data.ToStruct<VersionInfo>(offset);
        }
        catch (Exception)
        {
            return null;
        }
    }
}
