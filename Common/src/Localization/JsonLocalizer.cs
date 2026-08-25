using System.Collections.Immutable;
using System.Globalization;
using System.Net.Http.Headers;
using System.Text;
using Newtonsoft.Json;
using SmartFormat;

namespace Lytec.Common.Localization;

public class JsonLocalizer : Localizer
{
    public static string DefaultFileNameFormat { get; set; } = "lang/{LangName}.json";

    public string FileNameFormat { get; set; } = DefaultFileNameFormat;

    public CultureInfo Language { get; protected set; } = Thread.CurrentThread.CurrentUICulture;

    protected ImmutableArray<ImmutableDictionary<string, string>> Data { get; set; }

    protected virtual void Reload(CultureInfo lang)
    {
        var data = new List<ImmutableDictionary<string, string>>();
        var langs = new HashSet<string>();
        var de = JsonSerializer.Create(new JsonSerializerSettings()
        {
        });
        for (var la = lang; la != null; la = la.Parent)
        {
            if (!langs.Add(la.Name))
                continue;
            using var fs = File.OpenRead(Smart.Format(FileNameFormat, la.Name));
            using var sr = new StreamReader(fs, Encoding.UTF8);
            using var jr = new JsonTextReader(sr);
            var d = de.Deserialize<Dictionary<string, Dictionary<string, string>>>(jr);
            if (d == null)
                continue;
            data.Add(d.SelectMany(x => x.Value.Select(y => new
            {
                Key = CombineScopeAndKey(x.Key, y.Key),
                y.Value,
            })).ToImmutableDictionary(x => x.Key, x => x.Value));
        }
        Language = lang;
        Data = data.ToImmutableArray();
    }

    public void Reload() => Reload(Language);
    public void ChangeLang(CultureInfo lang) => Reload(lang);

    public override bool TryQuery(string key, out string Value)
    {
        var ds = Data;
        foreach (var d in ds)
        {
            if (d.TryGetValue(key, out var v) && !v.IsNullOrEmpty())
            {
                Value = v;
                return true;
            }
        }
        Value = "";
        return false;
    }
}
