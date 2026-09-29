using System.Collections;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace Lytec.Common.Localization;

public static class LocalizedLogPropertyNames
{
    public const string Message = "LocalizedMessage";
    public const string Scope = "LocalizationScope";
    public const string Key = "LocalizationKey";
    public const string LookupKey = "LocalizationLookupKey";
    public const string Culture = "LocalizationCulture";
    public const string FormatFailed = "LocalizationFormatFailed";
    public const string FormatException = "LocalizationFormatException";
}

public interface ILocalizedLogger<out TCategoryName> : ILogger<TCategoryName>
{
    void LogLocalized(
        LogLevel logLevel,
        EventId eventId,
        ILocalizeString message,
        Exception? exception = null);
}

public sealed class LocalizedLogger<TCategoryName>(
    ILocalizer localizer,
    ILogger<TCategoryName>? logger = null) : ILocalizedLogger<TCategoryName>
{
    private readonly ILocalizer localizer = localizer ?? throw new ArgumentNullException(nameof(localizer));
    private readonly ILogger<TCategoryName> logger = logger ?? NullLogger<TCategoryName>.Instance;

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
        => logger.BeginScope(state);

    public bool IsEnabled(LogLevel logLevel)
        => logger.IsEnabled(logLevel);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
        => logger.Log(logLevel, eventId, state, exception, formatter);

    public void LogLocalized(
        LogLevel logLevel,
        EventId eventId,
        ILocalizeString message,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(message);

        if (!logger.IsEnabled(logLevel))
            return;

        string text;
        string? cultureName;
        Exception? formatException = null;
        try
        {
            cultureName = localizer.CurrentCulture.Name;
            text = localizer.Format(message);
        }
        catch (Exception ex)
        {
            formatException = ex;
            cultureName = null;
            text = message.DefaultMessage ?? message.LookupKey;
        }

        var state = new LocalizedLogState(
            message,
            text,
            cultureName,
            formatException);
        logger.Log(
            logLevel,
            eventId,
            state,
            exception,
            static (value, _) => value.ToString());
    }

    private sealed class LocalizedLogState : IReadOnlyList<KeyValuePair<string, object?>>
    {
        private const string OriginalFormat = "{OriginalFormat}";
        private const string MessageTemplate = "{LocalizedMessage}";

        private readonly KeyValuePair<string, object?>[] properties;

        public LocalizedLogState(
            ILocalizeString message,
            string text,
            string? cultureName,
            Exception? formatException)
        {
            Text = text;

            var argumentCount = message.Arguments?.Count ?? 0;
            var exceptionPropertyCount = formatException is null ? 0 : 1;
            var values = new List<KeyValuePair<string, object?>>(
                7 + exceptionPropertyCount + argumentCount)
            {
                new(LocalizedLogPropertyNames.Message, text),
                new(LocalizedLogPropertyNames.Scope, message.Scope),
                new(LocalizedLogPropertyNames.Key, message.Key),
                new(LocalizedLogPropertyNames.LookupKey, message.LookupKey),
                new(LocalizedLogPropertyNames.Culture, cultureName),
                new(LocalizedLogPropertyNames.FormatFailed, formatException is not null),
            };

            if (formatException is not null)
            {
                values.Add(new(
                    LocalizedLogPropertyNames.FormatException,
                    $"{formatException.GetType().FullName}: {formatException.Message}"));
            }

            if (message.Arguments is not null)
            {
                foreach (var argument in message.Arguments)
                    values.Add(argument);
            }

            values.Add(new(OriginalFormat, MessageTemplate));
            properties = [.. values];
        }

        public string Text { get; }

        public int Count => properties.Length;

        public KeyValuePair<string, object?> this[int index] => properties[index];

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
            => ((IEnumerable<KeyValuePair<string, object?>>)properties).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => properties.GetEnumerator();

        public override string ToString() => Text;
    }
}

public static class LocalizedLoggerExtensions
{
    public static void LogLocalized<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        LogLevel logLevel,
        ILocalizeString message,
        Exception? exception = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        logger.LogLocalized(logLevel, default, message, exception);
    }

    public static void LogLocalizedTrace<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Trace, message, exception);

    public static void LogLocalizedDebug<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Debug, message, exception);

    public static void LogLocalizedInformation<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Information, message, exception);

    public static void LogLocalizedWarning<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Warning, message, exception);

    public static void LogLocalizedError<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Error, message, exception);

    public static void LogLocalizedCritical<TCategoryName>(
        this ILocalizedLogger<TCategoryName> logger,
        ILocalizeString message,
        Exception? exception = null)
        => logger.LogLocalized(LogLevel.Critical, message, exception);
}
