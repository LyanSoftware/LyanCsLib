using System.Collections.Concurrent;
using System.Globalization;
using Lytec.Common.Localization;
using Xunit;

namespace Test.Common.Localization;

public sealed class JsonLanguagePackServiceTest
{
    private const string Scope = "Test.Scope";

    [Fact]
    public async Task InitializeAsync_LoadsTargetParentEnglishAndEmbeddedFallbackPerKey()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("fr-CA.json", """
            {
              "Test.Scope": {
                "Target": "target",
                "Empty": "",
              },
            }
            """);
        directory.Write("fr.json", """
            {
              "Test.Scope": {
                "Parent": "parent",
                "Empty": "parent-for-empty"
              }
            }
            """);
        directory.Write("en.json", """
            {
              "Test.Scope": {
                "English": "english"
              }
            }
            """);

        var (localizer, service, _) = CreateService(directory.Path, "fr-CA");
        await service.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("target", Format(localizer, "Target"));
        Assert.Equal("parent", Format(localizer, "Parent"));
        Assert.Equal("parent-for-empty", Format(localizer, "Empty"));
        Assert.Equal("english", Format(localizer, "English"));
        Assert.Equal("embedded", Format(localizer, "Missing", "embedded"));
    }

    [Fact]
    public async Task DiscoverAsync_AcceptsEmptyPackAndTrailingCommas()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("en.json", "{}");
        directory.Write("zh-CN.json", """
            {
              "Test.Scope": {
                "Key": "值",
              },
            }
            """);

        var (_, service, _) = CreateService(directory.Path, "en");
        var languages = await service.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Equal(new[] { "en", "zh-CN" },
            languages.Select(static language => language.Id).OrderBy(static id => id));
    }

    [Fact]
    public async Task DiscoverAsync_SkipsInvalidNameAndInvalidContent()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("bad_name.json", "{}");
        directory.Write("fr.json", "[]");
        directory.Write("en.json", "{}");

        var (_, service, log) = CreateService(directory.Path, "en");
        var languages = await service.DiscoverAsync(TestContext.Current.CancellationToken);

        Assert.Single(languages);
        Assert.Equal("en", languages[0].Id);
        Assert.Equal(2, log.Entries.Count);
    }

    [Fact]
    public async Task DiscoverAsync_UsesLastDuplicateScopeAndKeyLikeNewtonsoftJson()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("en.json", """
            {
              "Test.Scope": { "Key": "first", "Key": "second" },
              "Test.Scope": { "Key": "last-scope" }
            }
            """);

        var (localizer, service, _) = CreateService(directory.Path, "en");
        await service.InitializeAsync(TestContext.Current.CancellationToken);

        Assert.Equal("last-scope", Format(localizer, "Key"));
    }

    [Fact]
    public async Task ChangeLanguageAsync_WhenRequestsOverlap_OnlyLatestRequestCommits()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("fr.json", "{ \"Test.Scope\": { \"Key\": \"fr\" } }");
        directory.Write("en.json", "{ \"Test.Scope\": { \"Key\": \"en\" } }");

        var preference = new RecordingPreferenceStore();
        var localizer = new JsonLocalizer();
        var log = new RecordingLogSink();
        var service = new JsonLanguagePackService(
            localizer,
            preference,
            log,
            directory.Path,
            CultureInfo.GetCultureInfo("en"));
        var cancellationToken = TestContext.Current.CancellationToken;
        await service.InitializeAsync(cancellationToken);

        var first = service.ChangeLanguageAsync("fr", cancellationToken);
        var last = service.ChangeLanguageAsync("en", cancellationToken);
        await Task.WhenAll(first, last);

        Assert.False(await first);
        Assert.True(await last);
        Assert.Equal("en", service.CurrentLanguageId);
        Assert.Equal("en", Format(localizer, "Key"));
        Assert.Equal(new[] { "en" }, preference.SavedIds);
    }

    [Fact]
    public async Task Observe_PublishesOnLanguageChangeAndStopsAfterDisposal()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("en.json", "{ \"Test.Scope\": { \"Key\": \"en\" } }");
        directory.Write("fr.json", "{ \"Test.Scope\": { \"Key\": \"fr\" } }");

        var (localizer, service, _) = CreateService(directory.Path, "en");
        var observer = new RecordingObserver();
        var subscription = localizer.Observe(new LocalizeString(
            Localizer.CombineScopeAndKey(Scope, "Key"),
            DefaultMessage: "embedded")).Subscribe(observer);

        var cancellationToken = TestContext.Current.CancellationToken;
        await service.InitializeAsync(cancellationToken);
        await service.ChangeLanguageAsync("fr", cancellationToken);
        subscription.Dispose();
        await service.ChangeLanguageAsync("en", cancellationToken);

        Assert.Equal(new[] { "embedded", "en", "fr" }, observer.Values);
    }

    [Fact]
    public async Task Format_UsesTheSelectedCultureForArguments()
    {
        using var directory = new TemporaryDirectory();
        directory.Write("fr-FR.json", "{}");
        directory.Write("en.json", "{ \"Test.Scope\": { \"Number\": \"{Value:N2}\" } }");

        var (localizer, service, _) = CreateService(directory.Path, "fr-FR");
        await service.InitializeAsync(TestContext.Current.CancellationToken);

        var value = localizer.Format(new LocalizeString(
            Localizer.CombineScopeAndKey(Scope, "Number"),
            new { Value = 1234.5 }));

        Assert.EndsWith(",50", value);
    }

    private static (JsonLocalizer Localizer, JsonLanguagePackService Service, RecordingLogSink Log)
        CreateService(string directory, string startupCulture)
    {
        var localizer = new JsonLocalizer();
        var log = new RecordingLogSink();
        var service = new JsonLanguagePackService(
            localizer,
            new NullLanguagePreferenceStore(),
            log,
            directory,
            CultureInfo.GetCultureInfo(startupCulture));
        return (localizer, service, log);
    }

    private static string Format(JsonLocalizer localizer, string key, string? fallback = null)
        => localizer.Format(new LocalizeString(
            Localizer.CombineScopeAndKey(Scope, key),
            DefaultMessage: fallback));

    private sealed class RecordingLogSink : ILocalizationLogSink
    {
        public ConcurrentQueue<(LocalizationLogLevel Level, ILocalizeString Message, Exception? Exception)>
            Entries { get; } = new();

        public void Write(
            LocalizationLogLevel level,
            ILocalizeString message,
            Exception? exception = null)
            => Entries.Enqueue((level, message, exception));
    }

    private sealed class RecordingPreferenceStore : ILanguagePreferenceStore
    {
        public List<string> SavedIds { get; } = [];

        public Task<string?> LoadAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<string?>(LanguageId.Auto);

        public Task SaveAsync(string languageId, CancellationToken cancellationToken = default)
        {
            SavedIds.Add(languageId);
            return Task.CompletedTask;
        }
    }

    private sealed class RecordingObserver : IObserver<string>
    {
        public List<string> Values { get; } = [];

        public void OnCompleted()
        {
        }

        public void OnError(Exception error)
            => throw error;

        public void OnNext(string value)
            => Values.Add(value);
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                $"Lytec.Localization.Tests.{Guid.NewGuid():N}");
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public void Write(string fileName, string content)
            => File.WriteAllText(System.IO.Path.Combine(Path, fileName), content);

        public void Dispose()
        {
            if (Directory.Exists(Path))
                Directory.Delete(Path, recursive: true);
        }
    }
}
