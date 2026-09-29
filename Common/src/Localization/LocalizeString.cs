using Lytec.Common;
namespace Lytec.Common.Localization;

public interface ILocalizeString
{
    string? Scope { get; }
    string Key { get; }
    string LookupKey { get; }
    IReadOnlyDictionary<string, object?>? Arguments { get; }
    string? DefaultMessage { get; }
}

[Serializable]
public record LocalizeString(
    string Key,
    string? DefaultMessage,
    IReadOnlyDictionary<string, object?>? Arguments = null
    ) : ILocalizeString
{
    public string? Scope { get; }

    public string Key { get; } = !Key.IsNullOrEmpty()
        ? Key
        : throw new ArgumentException("The localization key cannot be null or whitespace.", nameof(Key));

    public string LookupKey => Scope is null
        ? Key
        : Localizer.CombineScopeAndKey(Scope, Key);

    public string? DefaultMessage { get; } = DefaultMessage;

    public IReadOnlyDictionary<string, object?>? Arguments { get; } = Arguments;

    public LocalizeString(string Key, string? DefaultMessage, params (string Key, object? Value)[] Arguments)
        : this(Key, DefaultMessage, Arguments.ToDictionary()) { }

    public LocalizeString(string Key, IReadOnlyDictionary<string, object?>? Arguments = null) : this(Key, null, Arguments) { }
    public LocalizeString(string Key, params (string Key, object? Value)[] Arguments)
        : this(Key, Arguments.ToDictionary()) { }

    public LocalizeString(string Scope, string Key, string? DefaultMessage, IReadOnlyDictionary<string, object?>? Arguments = null)
        : this(Key, DefaultMessage, Arguments)
    {
        ArgumentException.ThrowIfNullOrEmpty(Scope);
        this.Scope = Scope;
    }

    public LocalizeString(string Scope, string Key, string? DefaultMessage, params (string Key, object? Value)[] Arguments)
        : this(Scope, Key, DefaultMessage, Arguments.ToDictionary()) { }

}
