using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Lytec.Common;

namespace Lytec.Protocol.LyLCDTvBrt;

public class BrightFixTable
{
    public const int ItemSize = 16;
    public const int ItemCount = 99;
    public const int TableSize = ItemSize * (ItemCount + 1); // [Duty Table]行加上99项数值
    public static Regex BrightFixTableItemRegex = new Regex(@"^Duty(?<Duty>\d{2})=\s*(?<Voltage>\d+)$", RegexOptions.Compiled);
    public const string BrightFixTableSectionName = "Duty Table";
    public const string BrightFixTableIdentifier = "[" + BrightFixTableSectionName + "]";

    public static Dictionary<int, int>? GetTable(byte[] data)
    {
        var lines = data.Select((v, i) => (i: i / ItemSize, v))
            .GroupBy(d => d.i)
            .Select(g => Config.Encoding.GetString(g.Select(d => d.v).TakeWhile(c => c != 0).ToArray()))
            .Take(ItemCount + 1)
            .ToList();
        if (lines.Count != ItemCount + 1 || lines[0] != BrightFixTableIdentifier)
            return null;
        var ld = lines
            .Skip(1)
            .Select((s, i) =>
            {
                var m = BrightFixTableItemRegex.Match(s);
                var duty = m.Success ? int.Parse(m.Groups["Duty"].Value) : -1;
                var voltage = m.Success ? int.Parse(m.Groups["Voltage"].Value) : -1;
                return (duty, voltage);
            })
            .ToList();
        if (ld.Any(d => d.duty < 0 || d.voltage < 0)
            || ld.GroupBy(d => d.duty).Any(g => g.Count() != 1))
            return null;
        var ret = ld.ToDictionary(d => d.duty, d => d.voltage);
        if (Enumerable.Range(1, ItemCount).All(ret.ContainsKey))
            return ret;
        return null;
    }

    public static bool GenTableIniData(IReadOnlyDictionary<int, int> vals, [NotNullWhen(true)] out string? Data)
    {
        Data = null;
        var table = Enumerable.Range(1, ItemCount)
            .Select(i => vals.TryGetValue(i, out var v) ? (i, v) : (i: -1, v: -1))
            .ToDictionary(kv => kv.i, kv => kv.v);
        if (table.Count != ItemCount)
            return false;
        Data = table.Select(kv => $"Duty{kv.Key:D2}={kv.Value,4}")
            .Prepend(BrightFixTableIdentifier)
            .JoinToString("\r\n");
        return true;
    }

    public static bool GenTableData(IReadOnlyDictionary<int, int> vals, [NotNullWhen(true)] out byte[]? Data)
    {
        Data = null;
        var table = Enumerable.Range(1, ItemCount)
            .Select(i => vals.TryGetValue(i, out var v) ? (i, v) : (i: -1, v: -1))
            .ToDictionary(kv => kv.i, kv => kv.v);
        if (table.Count != ItemCount)
            return false;
        Data = table.Select(kv => $"Duty{kv.Key:D2}={kv.Value,4}")
            .Prepend(BrightFixTableIdentifier)
            .SelectMany(s => Config.Encoding.GetBytes(s)
                .Append<byte>(0)
                .Concat(Enumerable.Repeat<byte>(0xFF, ItemSize))
                .Take(ItemSize)
            ).ToArray();
        return true;
    }

}
