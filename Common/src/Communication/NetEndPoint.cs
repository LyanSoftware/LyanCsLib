using System.Net;
using Newtonsoft.Json;

namespace Lytec.Common.Communication
{
    /// <summary>
    /// 网络终结点配置
    /// </summary>
    [JsonObject]
    [Serializable]
    public class NetEndPoint : IEquatable<NetEndPoint>, ICloneable
    {
        /// <summary>
        /// IP地址
        /// </summary>
        public string IP
        {
            get => Address.ToString();
            set => Address = IPAddress.Parse(value);
        }
        /// <summary>
        /// IP地址
        /// </summary>
        [JsonIgnore]
        public IPAddress Address
        {
            get => IPEndPoint.Address;
            set => IPEndPoint.Address = value;
        }
        /// <summary>
        /// 端口
        /// </summary>
        public int Port
        {
            get => IPEndPoint.Port;
            set => IPEndPoint.Port = value;
        }
        /// <summary>
        /// 网络终结点
        /// </summary>
        [JsonIgnore]
        public IPEndPoint IPEndPoint { get; set; } = new IPEndPoint(IPAddress.Any, 0);

        public NetEndPoint() { }

        public NetEndPoint(IPEndPoint ep) : this() => IPEndPoint = ep;

        public NetEndPoint(IPAddress address, int port) : this(new IPEndPoint(address, port)) { }

        /// <summary>
        /// 序列化
        /// </summary>
        /// <returns></returns>
        public virtual string Serialize() => Serialize(this);

        /// <summary>
        /// 创建副本
        /// </summary>
        /// <returns></returns>
        public virtual object Clone() => Clone(this);

        public override string ToString() => IPEndPoint.ToString();

        public override bool Equals(object? obj) => obj is NetEndPoint ep && Equals(ep);

        public bool Equals(NetEndPoint? other) => other is not null && EqualityComparer<IPEndPoint>.Default.Equals(IPEndPoint, other.IPEndPoint);

        public override int GetHashCode() => 1646792853 + EqualityComparer<IPEndPoint>.Default.GetHashCode(IPEndPoint);

        public static implicit operator IPEndPoint(NetEndPoint net) => net.IPEndPoint;
        public static implicit operator NetEndPoint(IPEndPoint ep) => new NetEndPoint(ep);

        public static bool operator ==(NetEndPoint left, NetEndPoint right) => EqualityComparer<NetEndPoint>.Default.Equals(left, right);

        public static bool operator !=(NetEndPoint left, NetEndPoint right) => !(left == right);

        /// <summary>
        /// 序列化
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static string Serialize(NetEndPoint ep) => JsonConvert.SerializeObject(ep);
        /// <summary>
        /// 反序列化
        /// </summary>
        /// <param name="json"></param>
        /// <returns></returns>
        public static NetEndPoint? Deserialize(string json) => JsonConvert.DeserializeObject<NetEndPoint>(json);
        /// <summary>
        /// 创建副本
        /// </summary>
        /// <param name="ep"></param>
        /// <returns></returns>
        public static NetEndPoint Clone(NetEndPoint ep) => new NetEndPoint(IPAddress.Parse(ep.Address.ToString()), ep.Port);
    }
}
