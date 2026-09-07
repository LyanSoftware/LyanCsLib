using System.Globalization;

namespace Lytec.Common.Localization;

public sealed class LanguagePackInfo
{
    public LanguagePackInfo(CultureInfo culture)
    {
        Culture = culture ?? throw new ArgumentNullException(nameof(culture));
    }

    public CultureInfo Culture { get; }

    public string Id => Culture.Name;

    public string DisplayName => $"{Culture.NativeName} ({Culture.EnglishName})";
}
