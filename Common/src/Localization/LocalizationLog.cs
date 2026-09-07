using System.Diagnostics;

namespace Lytec.Common.Localization;

public enum LocalizationLogLevel
{
    Debug,
    Information,
    Warning,
    Error,
    Fatal,
}

public interface ILocalizationLogSink
{
    void Write(
        LocalizationLogLevel level,
        ILocalizeString message,
        Exception? exception = null);
}

public sealed class DebugLocalizationLogSink : ILocalizationLogSink
{
    private readonly ILocalizer localizer;

    public DebugLocalizationLogSink(ILocalizer localizer)
    {
        this.localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    }

    public void Write(
        LocalizationLogLevel level,
        ILocalizeString message,
        Exception? exception = null)
    {
        var text = localizer.Format(message);
        Debug.WriteLine(exception is null
            ? $"[{level}] {text}"
            : $"[{level}] {text}{Environment.NewLine}{exception}");
    }
}
