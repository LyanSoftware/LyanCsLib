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
        string Key,
        string? DefaultMessage = null)
        => localizer.Observe(Key, DefaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string Key,
        object arguments,
        string? DefaultMessage = null)
        => localizer.Observe(Key, arguments, DefaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string scope,
        string Key,
        string? DefaultMessage = null)
        => localizer.Observe(scope, Key, DefaultMessage).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string scope,
        string Key,
        object arguments,
        string? DefaultMessage = null)
        => localizer.Observe(scope, Key, arguments, DefaultMessage).ToBinding();
}
