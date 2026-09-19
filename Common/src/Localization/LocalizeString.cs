using Lytec.Common;
namespace Lytec.Common.Localization;

public interface ILocalizeString
{
    string Key { get; }
    IReadOnlyDictionary<string, object?>? Arguments { get; }
    string? DefaultMessage { get; }
}

[Serializable]
public record LocalizeString(string Key, string? DefaultMessage, IReadOnlyDictionary<string, object?>? Arguments = null) : ILocalizeString
{
    public LocalizeString(string Key, string? DefaultMessage, params (string Key, object? Value)[] Arguments)
        : this(Key, DefaultMessage, Arguments.ToDictionary()) { }

    public LocalizeString(string Key, IReadOnlyDictionary<string, object?>? Arguments = null) : this(Key, null, Arguments) { }
    public LocalizeString(string Key, params (string Key, object? Value)[] Arguments)
        : this(Key, Arguments.ToDictionary()) { }
    
    public LocalizeString(string Scope, string Key, string? DefaultMessage, IReadOnlyDictionary<string, object?>? Arguments = null)
        : this(Localizer.CombineScopeAndKey(Scope, Key), DefaultMessage, Arguments) { }
    public LocalizeString(string Scope, string Key, string? DefaultMessage, params (string Key, object? Value)[] Arguments)
        : this(Scope, Key, DefaultMessage, Arguments.ToDictionary()) { }

}
