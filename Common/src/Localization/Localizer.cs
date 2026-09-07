using System.Globalization;
using SmartFormat;

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
        if (str is null)
            throw new ArgumentNullException(nameof(str));

        var hasTranslation = TryQuery(str.Key, out var format);
        format = hasTranslation ? format : str.DefaultMessage ?? str.Key;

        try
        {
            return str.Arguments is null
                ? format
                : Smart.Format(CurrentCulture, format, [str.Arguments]);
        }
        catch when (hasTranslation && str.DefaultMessage is not null)
        {
            return str.Arguments is null
                ? str.DefaultMessage
                : Smart.Format(CurrentCulture, str.DefaultMessage, [str.Arguments]);
        }
    }

    public IObservable<string> Observe(ILocalizeString str)
    {
        if (str is null)
            throw new ArgumentNullException(nameof(str));
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

    private sealed class LocalizedTextObservable(Localizer localizer, ILocalizeString value)
        : IObservable<string>
    {
        public IDisposable Subscribe(IObserver<string> observer)
        {
            if (observer is null)
                throw new ArgumentNullException(nameof(observer));

            EventHandler handler = (_, _) => observer.OnNext(localizer.Format(value));
            localizer.Changed += handler;
            observer.OnNext(localizer.Format(value));
            return new Subscription(localizer, handler);
        }
    }

    private sealed class Subscription(Localizer localizer, EventHandler handler) : IDisposable
    {
        private Localizer? source = localizer;
        private EventHandler? changedHandler = handler;

        public void Dispose()
        {
            var currentSource = Interlocked.Exchange(ref source, null);
            var currentHandler = Interlocked.Exchange(ref changedHandler, null);
            if (currentSource is not null && currentHandler is not null)
                currentSource.Changed -= currentHandler;
        }
    }
}
