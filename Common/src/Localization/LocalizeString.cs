namespace Lytec.Common.Localization;

public interface ILocalizeString
{
    string Key { get; }
    object? Arguments { get; }
    string? DefaultMessage { get; }
}

public record LocalizeString(string Key, object? Arguments = null, string? DefaultMessage = null) : ILocalizeString;
