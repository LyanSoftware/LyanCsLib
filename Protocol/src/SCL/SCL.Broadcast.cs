using System.Runtime.InteropServices;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using Lytec.Common.Data;
using Lytec.Common.Communication;
using Lytec.Common;
using static Lytec.Protocol.SCL.Constants;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json;
using System.ComponentModel;
using System.Diagnostics;

namespace Lytec.Protocol;

public static partial class SCL
{
    [JsonConverter(typeof(StringEnumConverter))]
    public enum BroadcastCommand : uint
    {
        [Description("搜索设备")]
        Seek = 0,
        [Description("修改详细配置")]
        SetConfig = 1,
        [Description("读取的详细配置数据")]
        ConfigData = 2,
        [Description("操作结果")]
        GetResult = 3,
    }

    public const int IdSize = 8;
    public const string Id_SuperCommSend = "SupLYTec";
    public const string Id_SuperCommRecv = "sUPlytEC";
    public const string Id_SCL2008Send = "LYTecSCL";
    public const string Id_SCL2008Recv = "lytECscl";

    [Serializable]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BroadcastHeader
    {
        public const int SizeConst = 20;

        static BroadcastHeader() => Debug.Assert(Marshal.SizeOf<BroadcastHeader>() == SizeConst);


        [field: MarshalAs(UnmanagedType.ByValArray, SizeConst = IdSize)]
        private byte[] IdentifierBytes { get; set; }
        public string Identifier
        {
            get => Encoding.ASCII.GetString(IdentifierBytes);
            set
            {
                if (IdentifierBytes == null)
                    IdentifierBytes = new byte[IdSize];
                Array.Copy(Encoding.ASCII.GetBytes(value), IdentifierBytes, IdSize);
            }
        }
        public BroadcastCommand Command { get; set; }
        public MacAddressPack Mac { get; set; }
    }

    public static class BroadcastSeek
    {
        [Serializable]
        [Endian(Endian.Little)]
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 1)]
        public class Info : IPackage
        {
            public const int SizeConst = BroadcastHeader.SizeConst + MacConfig.SizeConst + NetConfig.SizeConst;

            public BroadcastHeader Header { get; set; }

            public MacConfig MacConfig { get; set; }

            public NetConfig NetConfig { get; set; }

            public bool IsSCL2008 => Header.Identifier == Id_SCL2008Send || Header.Identifier == Id_SCL2008Recv;
            public bool IsSuperComm => Header.Identifier == Id_SuperCommSend || Header.Identifier == Id_SuperCommRecv;
            public bool IsAnswer => Header.Identifier == Id_SCL2008Recv || Header.Identifier == Id_SuperCommRecv;

            public byte[] Serialize() => this.ToBytes();

            public static Info? Deserialize(byte[] bytes, int offset = 0)
            {
                try
                {
                    var info = bytes.ToStruct<Info>(offset);
                    return (info.IsSCL2008 || info.IsSuperComm) && info.NetConfig.IsValid ? info : null;
                }
                catch (Exception)
                {
                    return null;
                }
            }
        }

        public static byte[] GetSeekPackage(bool isSCL2008 = true)
        => Encoding.ASCII.GetBytes((isSCL2008 ? Id_SCL2008Send : Id_SuperCommSend) + "\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00\x00");

        public record SeekAddress(IPEndPoint Local, IPEndPoint Remote, NetworkInterface NetworkInterface, bool IsInSameSubnet);

        public class SeekInfo
        {
            public MacAddress MacAddress { get; }
            public Info Info { get; }
            public IList<SeekAddress> Addresses { get; } = new List<SeekAddress>();

            public SeekInfo(MacAddress mac, Info info) => (MacAddress, Info) = (mac, info);
        }

        public static IReadOnlyDictionary<MacAddress, SeekInfo> Seek(ushort port = 28123, int timeoutMs = 5000)
        {
            var nis = NetworkInterface.GetAllNetworkInterfaces()
                .Where(ni => ni.OperationalStatus == OperationalStatus.Up && !ni.IsReceiveOnly)
                .Select(ni => (ni, ips: ni.GetIPProperties().UnicastAddresses.ToArray()))
                .ToArray();
            var socks = (from ni in nis
                         from ip in ni.ips
                         where ip.Address.AddressFamily == AddressFamily.InterNetwork
                         select (ni.ni, ip, so: new UdpClient(new IPEndPoint(ip.Address, 0)))
                         ).ToList();
            var cmd = GetSeekPackage();
            var infos = new Dictionary<MacAddress, SeekInfo>();
            void proc(bool localBroadcast)
            {
                var tasks = new List<Task>();
                foreach (var (_, ip, so) in socks)
                    tasks.Add(so.SendAsync(cmd, cmd.Length, new IPEndPoint(localBroadcast ? IPAddress.Broadcast : ip.GetBroadcastAddress(), port)));
                var timeout = DateTime.Now.AddMilliseconds(timeoutMs);
                while (timeout > DateTime.Now)
                {
                    var count1 = 0;
                    foreach (var (ni, ip, so) in socks)
                    {
                        count1++;
                        if (so.Available > 0)
                        {
                            var remote = new IPEndPoint(IPAddress.Any, 0);
                            if (Info.Deserialize(so.Receive(ref remote)) is Info info)
                            {
                                if (!infos.TryGetValue(info.MacConfig.MacAddress, out var seekinfo))
                                    infos[info.MacConfig.MacAddress] = seekinfo = new SeekInfo(info.MacConfig.MacAddress, info);
                                seekinfo.Addresses.Add(new SeekAddress(
                                    new IPEndPoint(ip.Address, 0),
                                    new IPEndPoint(localBroadcast ? IPAddress.Broadcast : ip.GetBroadcastAddress(), port),
                                    ni,
                                    ip.Address.IsInSameSubnet(ip.IPv4Mask, info.NetConfig.IP)
                                ));
                            }
                        }
                    }
                    if (count1 == 0)
                        Thread.Sleep(20);
                }
            }
            proc(false);
            proc(true);
            foreach (var so in socks)
            {
                so.so.Close();
                so.so.Dispose();
            }
            return infos;
        }
    }
}
