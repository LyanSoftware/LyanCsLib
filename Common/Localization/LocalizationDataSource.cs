using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace Lytec.Common.Localization
{
    public record LocalizationDataSource(string LangId, IReadOnlyDictionary<string, string> Data) : ILocalizationDataSource
    {
        public bool TryGetValue(string key, out string value) => Data.TryGetValue(key, out value);
    }

    public record JsonFileDataSource(string FilePath, string LangId, IReadOnlyDictionary<string, string> Data) : LocalizationDataSource(LangId, Data);

    public record JsonDataSource(string JsonData, string LangId, IReadOnlyDictionary<string, string> Data) : LocalizationDataSource(LangId, Data);

}

namespace Lytec.Common.Localization.Extensions
{
    public static class LocalizationDataSourceUtils
    {
        public static void AddLocalizationJsonData(this IServiceCollection collection, string langId, string jsonData)
        {
            collection.AddSingleton<ILocalizationDataSource, JsonDataSource>(_ =>
            {
                var dic = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonData) ?? throw new InvalidDataException();
                return new JsonDataSource(jsonData, langId, dic);
            });
        }
        public static void AddLocalizationJsonFile(this IServiceCollection collection, string langId, string filePath)
        => AddLocalizationJsonData(collection, langId, File.ReadAllText(filePath, Encoding.UTF8));
    }
}
