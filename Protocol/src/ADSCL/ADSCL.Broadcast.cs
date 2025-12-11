using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.ComponentModel;
using Lytec.Common.Data;
using System.Diagnostics;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using Lytec.Common.Communication;
using Lytec.Common;

namespace Lytec.Protocol;

public partial class ADSCL
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BroadcastCommand : uint
    {
        //[Description("搜索设备(旧版,部分兼容)")]
        //Seek_Deprecated = 0,
        //[Description("修改详细配置")]
        //SetConfig_Deprecated = 1,
        [Description("读取的详细配置数据")]
        ConfigData = 2,
        [Description("操作结果")]
        OpResult = 3,
        [Description("搜索设备")]
        Seek = 4,
        [Description("修改详细配置")]
        SetConfig = 5,
    }

    public static class BroadcastSeek
    {
        public class BCPack : IPackage
        {
            public bool IsAnswer { get; set; }
            public BroadcastCommand Command { get; set; }
            public MacAddress MacAddress { get; set; }
            public byte[] Data { get; set; } = Array.Empty<byte>();

            public const string Id_Send = "LYTecSCL";
            public const string Id_Recv = "lytECscl";

            static readonly byte[] SendIdBytes = Encoding.ASCII.GetBytes(Id_Send);
            static readonly byte[] RecvIdBytes = Encoding.ASCII.GetBytes(Id_Recv);

            public byte[] Serialize()
            {
                var buf = new List<byte>();
                if (IsAnswer)
                    buf.AddRange(RecvIdBytes);
                else buf.AddRange(SendIdBytes);
                var cmd = (int)Command;
                buf.AddRange(cmd.ToBytes(Endian.Little));
                buf.AddRange(MacAddress.Serialize());
                buf.Add(0);
                buf.Add(0);
                switch (Command)
                {
                    case BroadcastCommand.SetConfig:
                        buf.AddRange(Data);
                        break;
                }
                return buf.ToArray();
            }

            public static BCPack? Deserialize(byte[] bytes, int offset = 0)
            {
                var len = bytes.Length - offset;
                if (len < 12)
                    return null;
                var id = bytes.Skip(offset).Take(8).ToArray();
                offset += 8;
                len -= 8;
                bool isRcv;
                if (id.SequenceEqual(SendIdBytes))
                    isRcv = false;
                else if (id.SequenceEqual(RecvIdBytes))
                    isRcv = true;
                else return null;
                var cmd = bytes.Skip(offset).Take(4).ToArray().ToStruct<int>(Endian.Little);
                offset += 4;
                len -= 4;
                var mac = MacAddress.Empty;
                if (len >= 8)
                {
                    mac = new(bytes.Skip(offset).Take(6).ToArray());
                    offset += 8;
                    len -= 8;
                }
                switch (cmd)
                {
                    case (int)BroadcastCommand.Seek:
                        if (isRcv)
                            return null;
                        return Seek;
                    case (int)BroadcastCommand.ConfigData:
                        if (!isRcv || mac == MacAddress.Empty || len < NetConfig.SizeConst + NetInfo.SizeConst)
                            return null;
                        else
                        {
                            return new BCPack()
                            {
                                Command = BroadcastCommand.ConfigData,
                                IsAnswer = isRcv,
                                MacAddress = mac,
                                Data = bytes.Skip(offset).Take(1024).ToArray(),
                            };
                        }
                    case (int)BroadcastCommand.SetConfig:
                        if (isRcv || mac == MacAddress.Empty || len < NetConfig.SizeConst)
                            return null;
                        else
                        {
                            return new BCPack()
                            {
                                Command = BroadcastCommand.SetConfig,
                                IsAnswer = isRcv,
                                MacAddress = mac,
                                Data = bytes.Skip(offset).Take(1024).ToArray(),
                            };
                        }
                    case (int)BroadcastCommand.OpResult:
                        if (!isRcv || mac == MacAddress.Empty || len < 4)
                            return null;
                        else
                        {
                            var ret = bytes.Skip(offset).Take(4).ToArray();
                            if (ret.SequenceEqual(Enumerable.Repeat<byte>(0x00, 4))
                                || ret.SequenceEqual(Enumerable.Repeat<byte>(0x00, 4)))
                                return new BCPack()
                                {
                                    Command = BroadcastCommand.SetConfig,
                                    IsAnswer = isRcv,
                                    MacAddress = mac,
                                    Data = ret,
                                };
                            else return null;
                        }
                    default:
                        return null;
                }
            }

            public static readonly BCPack Seek = new BCPack()
            {
                IsAnswer = false,
                Command = BroadcastCommand.Seek,
            };
        }

        public record SeekAddress(IPEndPoint Local, IPEndPoint Remote, NetworkInterface NetworkInterface)
        {
            public static implicit operator SCL.BroadcastSeek.SeekAddress(SeekAddress addr) => new(addr.Local, addr.Remote, addr.NetworkInterface);
            public static implicit operator SeekAddress(SCL.BroadcastSeek.SeekAddress addr) => new(addr.Local, addr.Remote, addr.NetworkInterface);
        }

        [Serializable]
        [Endian(Endian.Little)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DHCPStatus
        {
            public const int SizeConst = 14;
            static DHCPStatus() => Debug.Assert(Marshal.SizeOf<DHCPStatus>() == SizeConst);

            public IPv4Address IP { get; set; }
            public IPv4Address SubnetMask { get; set; }
            public IPv4Address Gateway { get; set; }
            private readonly ushort unused;
        }

        [Serializable]
        [Endian(Endian.Little)]
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public class NetInfo : IPackage
        {
            public const int SizeConst = 44;
            static NetInfo() => Debug.Assert(Marshal.SizeOf<NetInfo>() == SizeConst);

            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] AppVer { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] FpgaVer { get; set; }
            [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
            public byte[] PcbVer { get; set; }
            public DHCPStatus LanDHCP;
            public DHCPStatus WLanDHCP;
            public ushort StatusBits;
            public ushort unused;

            public NetInfo()
            {
                AppVer = new byte[4];
                FpgaVer = new byte[4];
                PcbVer = new byte[4];
            }

            public byte[] Serialize() => this.ToBytes();

            public static NetInfo? Deserialize(byte[] bytes, int offset = 0)
            {
                try
                {
                    return bytes.ToStruct<NetInfo>(offset);
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public class SeekInfo
        {
            public MacAddress MacAddress { get; }
            public NetConfig NetConfig { get; }
            public NetInfo Info { get; set; }
            public IList<SeekAddress> Addresses { get; } = new List<SeekAddress>();

            public SeekInfo(MacAddress mac, NetConfig netcfg, NetInfo info) => (MacAddress, NetConfig, Info) = (mac, netcfg, info);
        }

        public static IReadOnlyDictionary<MacAddress, SeekInfo> Seek(ushort port = 28123, int timeoutMs = 5000)
        {
            var nis = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && !ni.IsReceiveOnly)
                .Select(ni => (ni, ips: ni.GetIPProperties().UnicastAddresses.ToArray()))
                .ToArray();
            var socks = (from ni in nis
                         from ip in ni.ips
                         from local in new bool[] { false, true }
                         where ip.Address.AddressFamily == AddressFamily.InterNetwork
                         select (ni.ni, ip, local, so: new UdpClient(new IPEndPoint(ip.Address, 0)))
                         ).ToList();
            var cmd = BCPack.Seek.Serialize();
            var infos = new Dictionary<MacAddress, SeekInfo>();
            var tasks = new List<Task>();
            foreach (var (_, ip, local, so) in socks)
                tasks.Add(so.SendAsync(cmd, cmd.Length, new IPEndPoint(local ? IPAddress.Broadcast : ip.GetBroadcastAddress(), port)));
            var timeout = DateTime.Now.AddMilliseconds(timeoutMs);
            while (timeout > DateTime.Now)
            {
                var count1 = 0;
                foreach (var (ni, ip, local, so) in socks)
                {
                    if (so.Available > 0)
                    {
                        var remote = new IPEndPoint(IPAddress.Any, 0);
                        try
                        {
                            if (BCPack.Deserialize(so.Receive(ref remote)) is BCPack pack
                                && pack.Command == BroadcastCommand.ConfigData)
                            {
                                var net = NetConfig.Deserialize(pack.Data);
                                var info = NetInfo.Deserialize(pack.Data, NetConfig.SizeConst) ?? new();
                                if (net != null)
                                {
                                    if (!infos.TryGetValue(net.MacAddress, out var seekinfo))
                                        infos[net.MacAddress] = seekinfo = new SeekInfo(net.MacAddress, net, info);
                                    seekinfo.Addresses.Add(new SeekAddress(
                                        new IPEndPoint(ip.Address, 0),
                                        new IPEndPoint(local ? IPAddress.Broadcast : ip.GetBroadcastAddress(), port),
                                        ni
                                        ));
                                }
                            }
                        }
                        catch (Exception) { }
                    }
                }
                if (count1 == 0)
                    Thread.Sleep(50);
            }
            foreach (var so in socks)
            {
                so.so.Close();
                so.so.Dispose();
            }
            return infos;
        }
    }
}
