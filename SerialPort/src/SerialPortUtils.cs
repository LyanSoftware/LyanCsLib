using System;
using System.Collections.Generic;
using System.Text;

namespace System.IO.Ports;

public static class SerialPortUtils_ly
{
    public static void Write(this SerialPort port, params byte[] bytes)
    => port.Write(bytes, 0, bytes.Length);
    public static void Write(this SerialPort port, IEnumerable<byte> bytes)
    {
        foreach (var b in bytes)
            port.Write(b);
    }
}
