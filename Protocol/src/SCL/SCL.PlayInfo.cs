using System.Runtime.InteropServices;
using Lytec.Common.Data;
using static Lytec.Protocol.SCL.Constants;

namespace Lytec.Protocol;

public static partial class SCL
{
    /// <summary>
    /// 播放状态
    /// </summary>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    [Endian(DefaultEndian)]
    public readonly struct PlayInfo
    {
        /// <summary>
        /// 当前驱动器
        /// </summary>
        public byte Driver { get; }
        /// <summary>
        /// 当前子目录序号
        /// </summary>
        public byte DirIndex { get; }
        /// <summary>
        /// 当前Program序号
        /// </summary>
        public byte ProgramIndex { get; }
        /// <summary>
        /// 区域1内当前Item序号
        /// </summary>
        public byte Screen1ProgramIndex { get; }
        /// <summary>
        /// 区域2内当前Item序号
        /// </summary>
        public byte Screen2ProgramIndex { get; }
        /// <summary>
        /// 区域3内当前Item序号
        /// </summary>
        public byte Screen3ProgramIndex { get; }
        /// <summary>
        /// 区域4内当前Item序号
        /// </summary>
        public byte Screen4ProgramIndex { get; }
    }
}
