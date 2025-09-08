using System.Collections;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public class JsonResult : IJsonData
{
    public int Code { get; set; }
    public string? Msg { get; set; }
    public IJsonData Data { get; set; }

    public JsonResult(int code, string? msg, IJsonData data)
    {
        Code = code;
        Msg = msg;
        Data = data;
    }
    public JsonResult(JsonResult other) : this(other.Code, other.Msg, other.Data) { }

    public T Add<T>(string key, T value) => Data != null ? Data.Add(key, value) : throw new InvalidOperationException();

    public object? Query(string jsonPath, JsonValueType type) => Data?.Query(JsonObj.FormatJsonPath(jsonPath), type);
    public IEnumerable QueryAll(string jsonPath, JsonValueType type) => Data?.QueryAll(JsonObj.FormatJsonPath(jsonPath), type) ?? throw new InvalidOperationException();


    public bool Exists(string jsonPath, JsonValueType type) => Data?.Exists(JsonObj.FormatJsonPath(jsonPath), type) ?? false;

    public T[] Add<T>(string key, params T[] values) => Data != null ? Data.Add(key, values) : throw new InvalidOperationException();
}
