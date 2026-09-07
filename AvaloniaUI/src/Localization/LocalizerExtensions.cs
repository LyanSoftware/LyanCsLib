using Avalonia;
using Avalonia.Data;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;

namespace Lytec.AvaloniaUI.Localization;

/// <summary>
/// Creates Avalonia bindings that re-evaluate when the localizer publishes a
/// language change. Avalonia owns and disposes the observable subscription with
/// the target binding.
/// </summary>
public static class LocalizerExtensions
{
    public static BindingBase Localize(
        this ILocalizer localizer,
        string key,
        string? defaultMessage = null)
        => localizer.Observe(key, defaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string key,
        object arguments,
        string? defaultMessage = null)
        => localizer.Observe(key, arguments, defaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string scope,
        string key,
        string? defaultMessage = null)
        => localizer.Observe(scope, key, defaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string scope,
        string key,
        object arguments,
        string? defaultMessage = null)
        => localizer.Observe(scope, key, arguments, defaultMessage).ToBinding();
}
