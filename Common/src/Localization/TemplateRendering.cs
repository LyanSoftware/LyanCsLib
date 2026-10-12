using System.Globalization;

namespace Lytec.Common.Localization;

public interface ILocalizationTemplateRenderer
{
    string Render(
        string template,
        IReadOnlyDictionary<string, object?>? variables,
        CultureInfo culture);
}

public sealed record LocalizationTemplateOptions
{
    public bool DebugMode { get; init; }
}

public sealed class LocalizationTemplateException : FormatException
{
    public LocalizationTemplateException(string message)
        : base(message)
    {
    }

    public LocalizationTemplateException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

public static class LocalizationTemplateFormatter
{
    public static string Format(object? value, string? format, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(culture);

        return value switch
        {
            null => string.Empty,
            IFormattable formattable => formattable.ToString(format, culture) ?? string.Empty,
            _ => value.ToString() ?? string.Empty,
        };
    }
}

public static class LocalizationTemplateRendererExtensions
{
    public static string Render(this ILocalizationTemplateRenderer rr, string template, CultureInfo culture, IReadOnlyDictionary<string, object?>? variables = null)
    => rr.Render(template, variables, culture);
    public static string Render(this ILocalizationTemplateRenderer rr, string template, CultureInfo culture, params (string Key, object? Value)[] variables)
    => rr.Render(template, variables.ToDictionary(), culture);
}
