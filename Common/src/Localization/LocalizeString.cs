namespace Lytec.Common.Localization;

public interface ILocalizeString
{
    string Key { get; }
    object? Arguments { get; }
    string? DefaultMessage { get; }
}

public record LocalizeString(string Key, object? Arguments, string? DefaultMessage) : ILocalizeString
{
    public LocalizeString(string Key, string? DefaultMessage) : this(Key, Arguments: null, DefaultMessage) { }

    public LocalizeString(string Scope, string Key, object? Arguments, string? DefaultMessage)
        : this(Localizer.CombineScopeAndKey(Scope, Key), Arguments, DefaultMessage)
    { }

    public LocalizeString(string Scope, string Key, string? DefaultMessage) : this(Scope, Key, null, DefaultMessage) { }
}
