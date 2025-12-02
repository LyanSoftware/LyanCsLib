using System.ComponentModel;
using System.Runtime.InteropServices;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Lytec.Protocol;

partial class SCL
{
    /// <summary>
    /// 能见度监测仪类型
    /// </summary>
    [JsonConverter(typeof(StringEnumConverter))]
    public enum VisMonitorType : byte
    {
        [Description("禁用")]
        Invalid = 0,
        [Description("天星智联TS AVS05 : 15秒平均")]
        VMT_TS_AVS05_15s = 1,
        [Description("天星智联TS AVS05 : 60秒平均")]
        VMT_TS_AVS05_60s = 2,
    }

    /// <summary>
    /// 能见度监测仪信息
    /// </summary>
    [Serializable]
    [JsonConverter(typeof(StringEnumConverter))]
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct VisMonitorInfo
    {
        /// <summary>
        /// 监测仪状态
        /// </summary>
        public ushort Status { get; set; }
        /// <summary>
        /// 当前环境能见度
        /// </summary>
        public ushort Vis { get; set; }
    }
}
