namespace Lytec.Common.Localization;

public interface ILanguagePreferenceStore
{
    Task<string?> LoadAsync(CancellationToken cancellationToken = default);

    Task SaveAsync(string languageId, CancellationToken cancellationToken = default);
}

public sealed class NullLanguagePreferenceStore : ILanguagePreferenceStore
{
    public Task<string?> LoadAsync(CancellationToken cancellationToken = default)
        => Task.FromResult<string?>(LanguageId.Auto);

    public Task SaveAsync(string languageId, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}
