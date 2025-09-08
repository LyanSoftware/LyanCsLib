using System;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Lytec.Common.Localization;

public abstract class Localization
{
    public enum EndOfLineFormat
    {
        DontChange = 0,
        EnvironmentDefault,
        CR,
        LF,
        CRLF,
        Windows = CRLF,
        Linux = LF,
        Mac = CR,
    }

    public bool AllowEscape { get; set; } = true;

    public EndOfLineFormat EOF { get; set; } = EndOfLineFormat.CRLF;

    public Func<string, string>? PostProcess { get; set; }

    public Func<string, string>? FormatInterpolateKey { get; set; } = key => $"{{{{{key}}}}}";

    public Localization()
    {
        PostProcess = str =>
        {
            if (AllowEscape)
                str = Regex.Unescape(str);
            var sb = new StringBuilder(str);
            switch (EOF)
            {
                default:
                case EndOfLineFormat.DontChange:
                    break;
                case EndOfLineFormat.EnvironmentDefault:
                case EndOfLineFormat.CR:
                case EndOfLineFormat.LF:
                case EndOfLineFormat.CRLF:
                    sb.Replace("\r\n", "\n");
                    sb.Replace("\n\r", "\n");
                    sb.Replace("\r", "\n");
                    switch (EOF)
                    {
                        default:
                            break;
                        case EndOfLineFormat.EnvironmentDefault:
                            sb.Replace("\n", Environment.NewLine);
                            break;
                        case EndOfLineFormat.CR:
                            sb.Replace("\n", "\r");
                            break;
                        case EndOfLineFormat.LF:
                            break;
                        case EndOfLineFormat.CRLF:
                            sb.Replace("\n", "\r\n");
                            break;
                    }
                    break;
            }
            return sb.ToString();
        };
    }

    public abstract bool Query(string key, out string Value);

    public virtual string Format(string str, IEnumerable<(string Key, string Value)> values)
    {
        var sb = new StringBuilder(str);
        foreach (var (key, value) in values)
            sb.Replace(FormatInterpolateKey?.Invoke(key) ?? key, value);
        return sb.ToString();
    }

    public string Format(string str, IEnumerable<KeyValuePair<string, string>> values)
    => Format(str, values.Select(kv => (kv.Key, kv.Value)));

    public string Format(string str, params KeyValuePair<string, string>[] values)
    => Format(str, values.AsEnumerable());
    
    public string Format(string str, params (string Key, string Value)[] values)
    => Format(str, values.AsEnumerable());

    public virtual string Q(string key, string defaultValue)
    {
        var v = Query(key, out var val) ? val : defaultValue;
        return PostProcess?.Invoke(v) ?? v;
    }

    public virtual string Q(string key)
    => Query(key, out var val) ? (PostProcess?.Invoke(val) ?? val) : key;

    /// <summary>
    /// 查询指定枚举值的本地化字符串
    /// </summary>
    public virtual string Q<T>(T value) where T : Enum
    {
        var path = $"{value.GetType().GetNestedClassName()}.{value}";
        return Query(path, out var val) ? (PostProcess?.Invoke(val) ?? val) : path;
    }

    /// <summary>
    /// 查询指定枚举值的本地化字符串
    /// </summary>
    public virtual string Q<T>(T value, string defaultValue) where T : Enum
    {
        var path = $"{value.GetType().GetNestedClassName()}.{value}";
        if (Query(path, out var val))
            return PostProcess?.Invoke(val) ?? val;
        if (!defaultValue.IsNullOrEmpty())
            return PostProcess?.Invoke(defaultValue) ?? defaultValue;
        return path;
    }
}
