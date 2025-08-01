using Newtonsoft.Json;

namespace Lytec.Common.Communication
{
    /// <summary>
    /// 通信配置
    /// </summary>
    [JsonObject]
    [Serializable]
    public class Config
    {
        public const int NoAddrCode = -1;

        /// <summary>
        /// 设备说明/备注
        /// </summary>
        public string Comment { get; set; } = "";

        /// <summary>
        /// 连接方式
        /// </summary>
        public ConnectType Mode { get; set; }

        /// <summary>
        /// 地址码
        /// </summary>
        public int AddrCode { get; set; } = NoAddrCode;

        /// <summary>
        /// 超时（毫秒）
        /// </summary>
        public int Timeout { get; set; } = 3000;

        /// <summary>
        /// 重试次数
        /// </summary>
        public int Retries { get; set; } = 1;

        /// <summary>
        /// 是否保持连接
        /// </summary>
        public virtual bool KeepConnection { get; set; }

        [JsonObject]
        public class UartConfig
        {
            /// <summary>
            /// 串口名称
            /// </summary>
            public string Name { get; set; } = "";
            /// <summary>
            /// 串口波特率
            /// </summary>
            public int Baudrate { get; set; }
        }

        public UartConfig Uart { get; } = new();

        [JsonObject]
        public class NetConfig
        {
            public NetEndPoint Local { get; } = new();

            public NetEndPoint Remote { get; } = new();
        }

        public NetConfig Net { get; } = new();

    }
}
