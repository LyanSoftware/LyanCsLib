using System;
using System.Globalization;
using Fluid;

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

    protected Localizer()
    {
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

        var hasTranslation = TryQuery(str.Key, out var format);
        format = hasTranslation ? format : str.DefaultMessage ?? str.Key;

        string render(string format)
        {
            if (str.Arguments == null)
                return format;
            if (new FluidParser().TryParse(format, out var template, out var error))
                return template.Render(new TemplateContext(str.Arguments) { CultureInfo = CurrentCulture });
            throw new FormatException($"Localization Format Failed: {error}");
        }
        try
        {
            return render(format);
        }
        catch when (hasTranslation && str.DefaultMessage is not null)
        {
            return render(str.DefaultMessage);
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
