using System.Net;
using Newtonsoft.Json;

namespace Lytec.Common.Communication
{
    /// <summary>
    /// 网络终结点配置
    /// </summary>
    [JsonObject]
    [Serializable]
    public class NetEndPointEx : NetEndPoint, IEquatable<NetEndPointEx>
    {
        /// <summary>
        /// 连接方式
        /// </summary>
        public ConnectType Mode { get; set; }

        public NetEndPointEx() : base() { }

        public NetEndPointEx(ConnectType mode) => Mode = mode;

        public NetEndPointEx(IPEndPoint ep, ConnectType mode = ConnectType.Invalid) : base(ep) => Mode = mode;

        public NetEndPointEx(IPAddress address, int port, ConnectType mode = ConnectType.Invalid) : base(address, port) => Mode = mode;

        public override string Serialize() => Serialize(this);

        public override object Clone() => Clone(this);

        public override string ToString() => $"[{Mode}]{base.ToString()}";

        public override bool Equals(object obj) => obj is NetEndPointEx n && Equals(n);

        public bool Equals(NetEndPointEx other) => base.Equals(other) && Mode == other.Mode;

        public override int GetHashCode()
        {
            int hashCode = 2021673416;
            hashCode = hashCode * -1521134295 + base.GetHashCode();
            hashCode = hashCode * -1521134295 + Mode.GetHashCode();
            return hashCode;
        }

        public static bool operator ==(NetEndPointEx left, NetEndPointEx right) => EqualityComparer<NetEndPointEx>.Default.Equals(left, right);

        public static bool operator !=(NetEndPointEx left, NetEndPointEx right) => !(left == right);

        /// <summary>
        /// 提取通信配置的本地端
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static NetEndPointEx GetLocal(Config config) => new NetEndPointEx(config.Net.Local, config.Mode);

        /// <summary>
        /// 提取通信配置的远程端
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public static NetEndPointEx GetRemote(Config config) => new NetEndPointEx(config.Net.Remote, config.Mode);

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static string Serialize(NetEndPointEx ep) => JsonConvert.SerializeObject(ep);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static new NetEndPointEx? Deserialize(string json) => JsonConvert.DeserializeObject<NetEndPointEx>(json);
        /// <summary>
        /// 创建副本
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static NetEndPointEx Clone(NetEndPointEx ep) => new NetEndPointEx(IPAddress.Parse(ep.Address.ToString()), ep.Port, ep.Mode);
    }
}
