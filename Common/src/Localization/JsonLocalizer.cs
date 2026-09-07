using System.Collections.Immutable;
using System.Globalization;

namespace Lytec.Common.Localization;

/// <summary>
/// Formats localized text from an atomically replaceable, ordered set of JSON
/// language-pack layers. Loading and validation are handled by
/// <see cref="ILanguagePackService"/>.
/// </summary>
public sealed class JsonLocalizer : Localizer
{
    private Snapshot snapshot = new(
        CultureInfo.InvariantCulture,
        ImmutableArray<ImmutableDictionary<string, string>>.Empty);

    public override CultureInfo CurrentCulture => snapshot.Culture;

    internal void Apply(
        CultureInfo culture,
        ImmutableArray<ImmutableDictionary<string, string>> layers)
    {
        if (culture is null)
            throw new ArgumentNullException(nameof(culture));

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
            if (layer.TryGetValue(key, out var value) && !value.IsNullOrEmpty())
            {
                Value = value;
                return true;
            }
        }

        Value = string.Empty;
        return false;
    }

    private sealed class Snapshot(
        CultureInfo culture,
        ImmutableArray<ImmutableDictionary<string, string>> layers)
    {
        public CultureInfo Culture { get; } = culture;
        public ImmutableArray<ImmutableDictionary<string, string>> Layers { get; } = layers;
    }
}
