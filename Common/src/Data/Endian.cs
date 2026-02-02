using System;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Lytec.Common.Data;


/// <summary>
/// 字节序
/// </summary>
public enum Endian
{
    /// <summary>
    /// 小端字节序
    /// </summary>
    Little,
    /// <summary>
    /// 大端字节序
    /// </summary>
    Big,
    /// <summary>
    /// 网络字节序
    /// </summary>
    Network = Big,
}
