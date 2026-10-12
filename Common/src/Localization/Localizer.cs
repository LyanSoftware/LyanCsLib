using System;
using System.Diagnostics;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lytec.Common.Localization;

public interface ILocalizer
{
    event EventHandler? Changed;

    CultureInfo CurrentCulture { get; }

    string Format(ILocalizeString str);

    IObservable<string> Observe(ILocalizeString str);
}

public abstract class Localizer : ILocalizer
{
    public const string ScopeConnector = ":";

    private readonly SynchronizationContext? notificationContext;
    private readonly ILocalizationTemplateRenderer templateRenderer;
    private readonly LocalizationTemplateOptions templateOptions;
    private readonly ILogger<Localizer>? logger;

    protected Localizer(
        ILocalizationTemplateRenderer templateRenderer,
        LocalizationTemplateOptions templateOptions,
        IEnumerable<ILogger<Localizer>>? logger = null)
    {
        this.templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        this.templateOptions = templateOptions ?? throw new ArgumentNullException(nameof(templateOptions));
        this.logger = logger?.LastOrDefault();
        notificationContext = SynchronizationContext.Current;
    }

    public event EventHandler? Changed;

    public abstract CultureInfo CurrentCulture { get; }

    public static string CombineScopeAndKey(string Scope, string Key)
    => Scope + ScopeConnector + Key;

    public abstract bool TryQuery(string key, out string Value);

    public virtual string Format(ILocalizeString str)
    {
        ArgumentNullException.ThrowIfNull(str);

        var hasTranslation = TryQuery(str.LookupKey, out var format);
        format = hasTranslation ? format : str.DefaultMessage ?? str.LookupKey;

        try
        {
            return templateRenderer.Render(format, str.Arguments, CurrentCulture);
        }
        catch (Exception exception)
        {
            LogTemplateFailure(str.LookupKey, "translation", exception);
            if (templateOptions.DebugMode || !hasTranslation || str.DefaultMessage is null)
                return format;
        }

        try
        {
            return templateRenderer.Render(str.DefaultMessage, str.Arguments, CurrentCulture);
        }
        catch (Exception exception)
        {
            LogTemplateFailure(str.LookupKey, "default message", exception);
            return str.DefaultMessage;
        }
    }

    public IObservable<string> Observe(ILocalizeString str)
    {
        ArgumentNullException.ThrowIfNull(str);
        return new LocalizedTextObservable(this, str);
    }

    protected void NotifyChanged()
    {
        var handler = Changed;
        if (handler is null)
            return;

        if (notificationContext is null || ReferenceEquals(notificationContext, SynchronizationContext.Current))
            handler(this, EventArgs.Empty);
        else
            notificationContext.Send(static state =>
            {
                var args = ((Localizer Localizer, EventHandler Handler))state!;
                args.Handler(args.Localizer, EventArgs.Empty);
            }, (this, handler));
    }

    private void LogTemplateFailure(string lookupKey, string templateSource, Exception exception)
    {
        var rendererType = templateRenderer.GetType().FullName ?? templateRenderer.GetType().Name;
        if (logger is not null)
        {
            logger.LogWarning(
                exception,
                "Localization template rendering failed for resource {LookupKey} using {RendererType} ({TemplateSource}).",
                lookupKey,
                rendererType,
                templateSource);
            return;
        }

        Debug.WriteLine(
            $"Localization template rendering failed for resource '{lookupKey}' " +
            $"using '{rendererType}' ({templateSource}): {exception}");
    }

    private record LocalizedTextObservable(Localizer Localizer, ILocalizeString Value)
        : IObservable<string>
    {
        public IDisposable Subscribe(IObserver<string> observer)
        {
            ArgumentNullException.ThrowIfNull(observer);

            EventHandler handler = (_, _) => observer.OnNext(Localizer.Format(Value));
            Localizer.Changed += handler;
            observer.OnNext(Localizer.Format(Value));
            return new Subscription(Localizer, handler);
        }
    }

    private record Subscription(Localizer Localizer, EventHandler Handler) : IDisposable
    {
        private Localizer? source = Localizer;
        private EventHandler? changedHandler = Handler;

        public void Dispose()
        {
            var currentSource = Interlocked.Exchange(ref source, null);
            var currentHandler = Interlocked.Exchange(ref changedHandler, null);
            if (currentSource is not null && currentHandler is not null)
                currentSource.Changed -= currentHandler;
        }
    }
}
