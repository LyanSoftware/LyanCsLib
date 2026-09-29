using System;
using System.Collections.Generic;
using System.Text;
using System.IO.Ports;

namespace Lytec.SerialPort;

public static class SerialPortExtensions
{
    public static void Write(this System.IO.Ports.SerialPort port, params byte[] bytes)
    => port.Write(bytes, 0, bytes.Length);
    public static void Write(this System.IO.Ports.SerialPort port, IEnumerable<byte> bytes)
    {
        foreach (var b in bytes)
            port.Write(b);
    }
}
