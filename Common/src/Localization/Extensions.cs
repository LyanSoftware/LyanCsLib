using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using static Lytec.Common.Localization.Extensions.Extensions;

namespace Lytec.Common.Localization.Extensions;

public static class Extensions
{
    public static IServiceCollection AddLocalization<T>(this IServiceCollection collection) where T : ILocalizer, new()
    => collection.AddSingleton<ILocalizer>(new T());
    public static IServiceCollection AddLocalization<T>(this IServiceCollection collection, T localizer) where T : ILocalizer
    => collection.AddSingleton<ILocalizer>(localizer);

    public static string CombineScopeAndKey(string Scope, string Key) => Localizer.CombineScopeAndKey(Scope, Key);
}

public static class LocalizerExtensions
{
    public static string Format(this ILocalizer localizer, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Key, Arguments, DefaultMessage));
    public static string Format(this ILocalizer localizer, string Key, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(Key, DefaultMessage: DefaultMessage));
    public static string Format(this ILocalizer localizer, string Scope, string Key, object Arguments, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(CombineScopeAndKey(Scope, Key), Arguments, DefaultMessage));
    public static string Format(this ILocalizer localizer, string Scope, string Key, string? DefaultMessage = null)
    => localizer.Format(new LocalizeString(CombineScopeAndKey(Scope, Key), DefaultMessage: DefaultMessage));
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
    => ex.Localize(new LocalizeString(CombineScopeAndKey(Scope, Key), Arguments, DefaultMessage));
    public static T Localize<T>(this T ex, string Scope, string Key, string? DefaultMessage = null) where T : Exception
    => ex.Localize(new LocalizeString(CombineScopeAndKey(Scope, Key), DefaultMessage: DefaultMessage));

}
