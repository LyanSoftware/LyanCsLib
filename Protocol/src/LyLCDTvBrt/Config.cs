using System;
using System.Collections.Generic;
using System.Text;
using static Lytec.Protocol.LyLCDTvBrt.ProgramInfo;

namespace Lytec.Protocol.LyLCDTvBrt;

public class Config
{
    public static Encoding Encoding { get; set; } = Encoding.GetEncoding("gbk");

    public static string GetConfig(ReadOnlySpan<byte> data)
    {
        var buf = new List<byte>(data.Length);
        foreach (var b in data)
        {
            if (b != '\0')
                buf.Add(b);
            else break;
        }
        return Encoding.GetString(buf.ToArray());
    }

    public static byte[] GenConfigData(string config)
    => Encoding.GetBytes(config).Take(ConfigSize - 1).Append<byte>(0).ToArray();
}
