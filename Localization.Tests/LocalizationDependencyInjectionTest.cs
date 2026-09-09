using System.Globalization;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Extensions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Test.Common.Localization;

public sealed class LocalizationDependencyInjectionTest
{
    [Fact]
    public void AddLocalization_MapsConcreteAndInterfaceToTheSameSingleton()
    {
        var services = new ServiceCollection();
        services.AddLocalization<TestLocalizer>();

        using var provider = BuildProvider(services);

        Assert.Same(
            provider.GetRequiredService<TestLocalizer>(),
            provider.GetRequiredService<ILocalizer>());
    }

    [Fact]
    public void AddLocalizationInstance_MapsConcreteAndInterfaceToTheGivenInstance()
    {
        var localizer = new TestLocalizer();
        var services = new ServiceCollection();
        services.AddLocalization(localizer);

        using var provider = BuildProvider(services);

        Assert.Same(localizer, provider.GetRequiredService<TestLocalizer>());
        Assert.Same(localizer, provider.GetRequiredService<ILocalizer>());
    }

    [Fact]
    public void AddJsonLocalization_RegistersAValidatedGraphAndPreservesOverrides()
    {
        var preference = new TestLanguagePreferenceStore();
        var log = new TestLocalizationLogSink();
        var languageDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N"));
        var startupCulture = CultureInfo.GetCultureInfo("fr-CA");
        var languagePackSource = new EmptyLanguagePackSource();
        var services = new ServiceCollection();

        services.AddSingleton<ILanguagePreferenceStore>(preference);
        services.AddSingleton<ILocalizationLogSink>(log);
        services.AddJsonLocalization(options =>
        {
            options.LanguageDirectory = languageDirectory;
            options.LanguagePackSource = languagePackSource;
            options.StartupCulture = startupCulture;
        });

        using var provider = BuildProvider(services);
        var concreteLocalizer = provider.GetRequiredService<JsonLocalizer>();
        var languagePacks = provider.GetRequiredService<ILanguagePackService>();

        Assert.Same(concreteLocalizer, provider.GetRequiredService<ILocalizer>());
        Assert.Same(languagePacks, provider.GetRequiredService<ILanguagePackService>());
        Assert.Same(preference, provider.GetRequiredService<ILanguagePreferenceStore>());
        Assert.Same(log, provider.GetRequiredService<ILocalizationLogSink>());
        Assert.Same(languagePackSource, provider.GetRequiredService<ILanguagePackSource>());
        Assert.Same(languagePackSource, Assert.IsType<JsonLanguagePackService>(languagePacks).LanguagePackSource);
        Assert.Equal(Path.GetFullPath(languageDirectory), languagePacks.LanguageDirectory);
        Assert.Equal(startupCulture, languagePacks.StartupCulture);
    }

    private static ServiceProvider BuildProvider(IServiceCollection services)
        => services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });

    private sealed class TestLocalizer : Localizer
    {
        public override CultureInfo CurrentCulture => CultureInfo.InvariantCulture;

        public override bool TryQuery(string key, out string value)
        {
            value = string.Empty;
            return false;
        }
    }

    private sealed class TestLanguagePreferenceStore : ILanguagePreferenceStore
    {
        public Task<string?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(LanguageId.Auto);

        public Task SaveAsync(string languageId, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }

    private sealed class TestLocalizationLogSink : ILocalizationLogSink
    {
        public void Write(
            LocalizationLogLevel level,
            ILocalizeString message,
            Exception? exception = null)
        {
        }
    }

    private sealed class EmptyLanguagePackSource : ILanguagePackSource
    {
        public string Description => "empty";

        public IEnumerable<LanguagePackResource> Enumerate(
            CancellationToken cancellationToken = default)
            => [];
    }
}
