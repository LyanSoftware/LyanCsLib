using System.Collections;
using Newtonsoft.Json.Linq;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public class JsonObj : JObject, IJsonData
{
    public static string FormatJsonPath(string path) => path.StartsWith("$.") ? path : $"$.{path}";

    public JsonObj() { }
    public JsonObj(JObject obj) : base(obj) { }
    public JsonObj(params object[] objects) : base(objects) { }
    public JsonObj(params (string Key, object Value)[] objects) : this(objects.AsEnumerable()) { }
    public JsonObj(params KeyValuePair<string, object>[] objects) : this(objects.AsEnumerable()) { }
    public JsonObj(IEnumerable<(string Key, object Value)> objects)
    {
        foreach (var (k, v) in objects)
            Add(k, v);
    }
    public JsonObj(IEnumerable<KeyValuePair<string, object>> objects) : this(objects.Select(kv => (kv.Key, kv.Value))) { }

    public bool Exists(string jsonPath, JsonValueType type)
    {
        jsonPath = FormatJsonPath(jsonPath);
        switch (type)
        {
            case JsonValueType.String:
            case JsonValueType.Int:
            case JsonValueType.Bool:
            case JsonValueType.Float:
                return SelectToken(jsonPath) is JValue v && v.Type == type switch
                {
                    JsonValueType.String => JTokenType.String,
                    JsonValueType.Int => JTokenType.Integer,
                    JsonValueType.Bool => JTokenType.Boolean,
                    JsonValueType.Float => JTokenType.Float,
                    _ => throw new ArgumentException(),
                };
            default:
            case JsonValueType.Object:
                return SelectToken(jsonPath) is JObject;
            case JsonValueType.Array:
                return SelectToken(jsonPath) is JArray;
        }
    }

    public T Add<T>(string key, T value)
    {
        switch (value)
        {
            case null:
                this[key] = null;
                break;
            case string str:
                this[key] = new JValue(str);
                break;
            case int i:
                this[key] = new JValue(i);
                break;
            case long l:
                this[key] = new JValue(l);
                break;
            case decimal d:
                this[key] = new JValue(d);
                break;
            case float f:
                this[key] = new JValue(f);
                break;
            case bool b:
                this[key] = new JValue(b);
                break;
            default:
                if (value is JArray jarr)
                    this[key] = jarr;
                else if (typeof(T).IsArray && value is Array arr)
                    this[key] = new JArray((object[])arr);
                else if (value is JObject jobj)
                    this[key] = jobj;
                else this[key] = new JObject(value);
                break;
        }
        return value;
    }

    public T[] Add<T>(string key, params T[] values)
    {
        this[key] = new JArray(values);
        return values;
    }

    public object? Query(string jsonPath, JsonValueType type)
    {
        jsonPath = FormatJsonPath(jsonPath);
        switch (type)
        {
            case JsonValueType.String:
            case JsonValueType.Bool:
                return SelectToken(jsonPath) is JValue v && v.Type == type switch
                {
                    JsonValueType.String => JTokenType.String,
                    JsonValueType.Int => JTokenType.Integer,
                    JsonValueType.Bool => JTokenType.Boolean,
                    JsonValueType.Float => JTokenType.Float,
                    _ => throw new ArgumentException(),
                } ? v.Value : null;
            case JsonValueType.Int:
                return SelectToken(jsonPath) is JValue lv && lv.Type == JTokenType.Integer && lv.Value is long l ? (int)l : null;
            case JsonValueType.Float:
                return SelectToken(jsonPath) is JValue fv && fv.Type == JTokenType.Float ? (fv.Value is double d ? d : (fv.Value is float f ? (double?)f : null)) : null;
            default:
            case JsonValueType.Object:
                return SelectToken(jsonPath) is JObject obj ? obj : null;
            case JsonValueType.Array:
                return SelectToken(jsonPath) is JArray arr ? arr : null;
        }
    }

    public IEnumerable QueryAll(string jsonPath, JsonValueType type)
    {
        jsonPath = FormatJsonPath(jsonPath);
        var q = SelectTokens(jsonPath);
        switch (type)
        {
            case JsonValueType.String:
            case JsonValueType.Int:
            case JsonValueType.Bool:
            case JsonValueType.Float:
                return q.Where(v => v.Type == type switch
                {
                    JsonValueType.String => JTokenType.String,
                    JsonValueType.Int => JTokenType.Integer,
                    JsonValueType.Bool => JTokenType.Boolean,
                    JsonValueType.Float => JTokenType.Float,
                    _ => throw new ArgumentException(),
                });
            default:
            case JsonValueType.Object:
                return q.Where(v => v.Type == JTokenType.Object);
            case JsonValueType.Array:
                return q.Where(v => v.Type == JTokenType.Array);
        }
    }
}
