using System.Globalization;

namespace Lytec.Common.Localization;

public sealed class JsonLocalizationOptions
{
    /// <summary>
    /// Gets or sets the language-pack directory. A null value uses
    /// <c>AppContext.BaseDirectory/lang</c>.
    /// </summary>
    public string? LanguageDirectory { get; set; }

    /// <summary>
    /// Gets or sets the UI culture captured as the automatic language. A null
    /// value captures <see cref="CultureInfo.CurrentUICulture"/> when the
    /// language-pack service is created.
    /// </summary>
    public CultureInfo? StartupCulture { get; set; }
}
