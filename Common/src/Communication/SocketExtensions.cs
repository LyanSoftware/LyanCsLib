using Lytec.Common.Serialization;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;

namespace Lytec.Common.Communication
{
    public static class SocketExtensions
    {
        public static void SetTcpKeepAlive(
            this Socket socket,
            bool enabled,
            int keepAliveTimeSeconds,
            int keepAliveIntervalSeconds)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // Windows:
                // struct tcp_keepalive
                // {
                //     uint onoff;
                //     uint keepalivetime;      // ms
                //     uint keepaliveinterval;  // ms
                // }

                var values = new byte[12];
                var span = values.AsSpan();
                FixedBinaryPrimitives.WriteUInt32(span.Slice(0, 4), enabled ? 1u : 0u, Data.EndianUtils.LocalEndian);
                FixedBinaryPrimitives.WriteUInt32(span.Slice(4, 4), checked((uint)keepAliveTimeSeconds * 1000), Data.EndianUtils.LocalEndian);
                FixedBinaryPrimitives.WriteUInt32(span.Slice(8, 4), checked((uint)keepAliveIntervalSeconds * 1000), Data.EndianUtils.LocalEndian);

                socket.IOControl(
                    IOControlCode.KeepAliveValues,
                    values,
                    null);

                return;
            }

            // Linux/macOS 等
            socket.SetSocketOption(
                SocketOptionLevel.Socket,
                SocketOptionName.KeepAlive,
                enabled);

            if (!enabled)
                return;

            // netstandard2.0 没有这些枚举名字，但当前 .NET
            // SocketOptionName 中规定的值就是 3 和 17。
            const SocketOptionName TcpKeepAliveTime =
                (SocketOptionName)3;

            const SocketOptionName TcpKeepAliveInterval =
                (SocketOptionName)17;

            socket.SetSocketOption(
                SocketOptionLevel.Tcp,
                TcpKeepAliveTime,
                keepAliveTimeSeconds);

            socket.SetSocketOption(
                SocketOptionLevel.Tcp,
                TcpKeepAliveInterval,
                keepAliveIntervalSeconds);
        }

    }
}
