using System.Globalization;

namespace Lytec.Common.Localization;

public record LanguagePackInfo(CultureInfo Culture)
{
    public string Id => Culture.Name;

    public string DisplayName => Culture.NativeName != Culture.EnglishName ? $"{Culture.NativeName} ({Culture.EnglishName})" : Culture.EnglishName;
}
