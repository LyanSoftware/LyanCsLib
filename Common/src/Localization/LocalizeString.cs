namespace Lytec.Common.Localization;

public interface ILocalizeString
{
    string Key { get; }
    object? Arguments { get; }
    string? DefaultMessage { get; }
}

public record LocalizeString(string Key, object? Arguments = null, string? DefaultMessage = null) : ILocalizeString
{
    public LocalizeString(string Scope, string Key, object? Arguments = null, string? DefaultMessage = null)
        : this(Localizer.CombineScopeAndKey(Scope, Key), Arguments, DefaultMessage)
    { }
}
