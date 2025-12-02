using System.Collections;
using System.Runtime.InteropServices;
using System.Diagnostics;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    [Serializable]
    [Endian(DefaultEndian)]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RouteData : IEnumerable, IEnumerable<ushort>, IReadOnlyList<ushort>, IPackage
    {
        public const int Length = 1024;
        public const int SizeConst = 2048;

        static RouteData() => Debug.Assert(Marshal.SizeOf<RouteData>() == SizeConst);

        public ushort[] Data
        {
            get
            {
                if (_Data == null)
                    _Data = new ushort[Length];
                return _Data;
            }
        }

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = Length)]
        private ushort[] _Data;

        public ushort this[int index] { get => Data[index]; set => Data[index] = value; }

        public RouteData(IEnumerable<ushort> data)
        {
            var arr = data.Take(Length).ToArray();
            if (arr.Length != Length)
                throw new ArgumentException();
            _Data = arr;
        }

        IEnumerator IEnumerable.GetEnumerator() => Data.GetEnumerator();

        public byte[] Serialize() => this.ToBytes();
        public static RouteData Deserialize(byte[] bytes, int offset = 0) => bytes.ToStruct<RouteData>(offset);

        public int Count => Data.Length;

        public IEnumerator<ushort> GetEnumerator() => ((IEnumerable<ushort>)Data).GetEnumerator();
    }
}
