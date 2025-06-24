using System.Globalization;

namespace Lytec.Common.Localization
{
    public interface ILocalizationDataSource : DI.IService
    {
        string LangId { get; }
        bool TryGetValue(string key, out string value);
    }
}
