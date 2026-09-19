using Avalonia;
using Avalonia.Data;
using Lytec.Common;
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
        string? DefaultMessage,
        IReadOnlyDictionary<string, object?>? Arguments = null)
        => localizer.Observe(Key, DefaultMessage, Arguments).ToBinding();
    public static BindingBase Localize(
        this ILocalizer localizer,
        string Key,
        string? DefaultMessage,
        params (string Key, object? Value)[] Arguments)
        => localizer.Observe(Key, DefaultMessage, Arguments).ToBinding();

    public static BindingBase Localize(
        this ILocalizer localizer,
        string Scope,
        string Key,
        string? DefaultMessage,
        IReadOnlyDictionary<string, object?>? Arguments = null)
        => localizer.Observe(Scope, Key, DefaultMessage, Arguments).ToBinding();
    public static BindingBase Localize(
        this ILocalizer localizer,
        string Scope,
        string Key,
        string? DefaultMessage,
        params (string Key, object? Value)[] Arguments)
        => localizer.Observe(Scope, Key, DefaultMessage, Arguments).ToBinding();
}
