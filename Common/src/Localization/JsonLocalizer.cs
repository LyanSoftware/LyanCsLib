using System.Collections.Immutable;
using System.Globalization;
using Microsoft.Extensions.Logging;

namespace Lytec.Common.Localization;

using System;

/// <summary>
/// Formats localized text from an atomically replaceable, ordered set of JSON
/// language-pack layers. Loading and validation are handled by
/// <see cref="ILanguagePackService"/>.
/// </summary>
public sealed class JsonLocalizer : Localizer
{
#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0301:简化集合初始化", Justification = "<挂起>")]
#endif
    private Snapshot snapshot = new(
        CultureInfo.InvariantCulture,
        ImmutableArray<ImmutableDictionary<string, string>>.Empty);

    public JsonLocalizer(
        ILocalizationTemplateRenderer templateRenderer,
        LocalizationTemplateOptions templateOptions,
        IEnumerable<ILogger<Localizer>>? logger = null)
        : base(templateRenderer, templateOptions, logger)
    {
    }

    public override CultureInfo CurrentCulture => snapshot.Culture;

#if NET8_0_OR_GREATER
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Style", "IDE0301:简化集合初始化", Justification = "<挂起>")]
#endif
    internal void Apply(
        CultureInfo culture,
        ImmutableArray<ImmutableDictionary<string, string>> layers)
    {
        ArgumentNullException.ThrowIfNull(culture);

        snapshot = new Snapshot(
            culture,
            layers.IsDefault
                ? ImmutableArray<ImmutableDictionary<string, string>>.Empty
                : layers);
        NotifyChanged();
    }

    public override bool TryQuery(string key, out string Value)
    {
        var current = snapshot;
        foreach (var layer in current.Layers)
        {
            if (layer.TryGetValue(key, out var value))
            {
                Value = value;
                return true;
            }
        }

        Value = string.Empty;
        return false;
    }

    private record Snapshot(
        CultureInfo Culture,
        ImmutableArray<ImmutableDictionary<string, string>> Layers);
}
