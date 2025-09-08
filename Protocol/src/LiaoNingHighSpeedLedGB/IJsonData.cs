using System.Collections;

namespace Lytec.Protocol.LiaoNingHighSpeedLedGB;

public interface IJsonData
{
    T Add<T>(string key, T value);
    T[] Add<T>(string key, params T[] values);
    object? Query(string jsonPath, JsonValueType type);
    IEnumerable QueryAll(string jsonPath, JsonValueType type);
    bool Exists(string jsonPath, JsonValueType type);
}
