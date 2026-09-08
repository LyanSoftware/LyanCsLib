using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using static Lytec.Common.Localization.Extensions.Extensions;

namespace Lytec.Common.Localization.Extensions;

public static class Extensions
{
    public static IServiceCollection AddLocalization<T>(this IServiceCollection collection)
        where T : class, ILocalizer
    {
        collection.AddSingleton<T>();
        collection.AddSingleton<ILocalizer>(
            static services => services.GetRequiredService<T>());
        return collection;
    }

    public static IServiceCollection AddLocalization<T>(
        this IServiceCollection collection,
        T localizer)
        where T : class, ILocalizer
    {
        if (localizer is null)
            throw new ArgumentNullException(nameof(localizer));

        collection.AddSingleton(localizer);
        collection.AddSingleton<ILocalizer>(
            static services => services.GetRequiredService<T>());
        return collection;
    }

    public static IServiceCollection AddJsonLocalization(
        this IServiceCollection collection,
        Action<JsonLocalizationOptions>? configure = null)
    {
        if (collection is null)
            throw new ArgumentNullException(nameof(collection));

        var options = new JsonLocalizationOptions();
        configure?.Invoke(options);

        collection.TryAddSingleton(options);
        collection.TryAddSingleton<JsonLocalizer>();
        collection.TryAddSingleton<ILocalizer>(
            static services => services.GetRequiredService<JsonLocalizer>());
        collection.TryAddSingleton<ILanguagePreferenceStore, NullLanguagePreferenceStore>();
        collection.TryAddSingleton<ILocalizationLogSink, DebugLocalizationLogSink>();
        collection.TryAddSingleton<ILanguagePackService>(static services =>
        {
            var configuredOptions = services.GetRequiredService<JsonLocalizationOptions>();
            return new JsonLanguagePackService(
                services.GetRequiredService<JsonLocalizer>(),
                services.GetRequiredService<ILanguagePreferenceStore>(),
                services.GetRequiredService<ILocalizationLogSink>(),
                configuredOptions.LanguageDirectory,
                configuredOptions.StartupCulture);
        });

        return collection;
    }
}

public static class LocalizerExtensions
{
    public static string Format(this ILocalizer localizer, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Key, Arguments, DefaultMessage));
    public static string Format(this ILocalizer localizer, string Key, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Key, DefaultMessage: DefaultMessage));
    public static string Format(this ILocalizer localizer, string Scope, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Scope, Key, Arguments, DefaultMessage));
    public static string Format(this ILocalizer localizer, string Scope, string Key, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Scope, Key, DefaultMessage: DefaultMessage));

    public static IObservable<string> Observe(this ILocalizer localizer, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Observe(new LocalizeString(Key, Arguments, DefaultMessage));
    public static IObservable<string> Observe(this ILocalizer localizer, string Key, string? DefaultMessage = null)
    => localizer.Observe(new LocalizeString(Key, DefaultMessage: DefaultMessage));
    public static IObservable<string> Observe(this ILocalizer localizer, string Scope, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Observe(new LocalizeString(Scope, Key, Arguments, DefaultMessage));
    public static IObservable<string> Observe(this ILocalizer localizer, string Scope, string Key, string? DefaultMessage = null)
    => localizer.Observe(new LocalizeString(Scope, Key, DefaultMessage: DefaultMessage));
}

public static class ExceptionExtensions
{
    private static string LocalizeDataKey { get; } = "Lytec.Localization.LocalizeData";

    public static ILocalizeString? GetLocalizedMessage(this Exception ex)
    {
        return ex.Data.Contains(LocalizeDataKey) ? ex.Data[LocalizeDataKey] as ILocalizeString : null;
    }

    public static T Localize<T>(this T ex, ILocalizeString msg) where T : Exception
    {
        ex.Data[LocalizeDataKey] = msg;
        return ex;
    }

    public static T Localize<T>(this T ex, string Key, object Arguments, string? DefaultMessage = null) where T : Exception
    => ex.Localize(new LocalizeString(Key, Arguments, DefaultMessage));
    public static T Localize<T>(this T ex, string Key, string? DefaultMessage = null) where T : Exception
    => ex.Localize(new LocalizeString(Key, DefaultMessage: DefaultMessage));
    public static T Localize<T>(this T ex, string Scope, string Key, object Arguments, string? DefaultMessage = null) where T : Exception
    => ex.Localize(new LocalizeString(Scope, Key, Arguments, DefaultMessage));
    public static T Localize<T>(this T ex, string Scope, string Key, string? DefaultMessage = null) where T : Exception
    => ex.Localize(new LocalizeString(Scope, Key, DefaultMessage: DefaultMessage));

}
