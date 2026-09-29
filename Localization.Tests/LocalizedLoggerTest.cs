using System.Globalization;
using Lytec.Common.Localization;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Test.Common.Localization;

public sealed class LocalizedLoggerTest
{
    private const string Scope = "Test.Scope";

    [Fact]
    public void LogLocalized_UsesCurrentLanguageAndPreservesStructuredProperties()
    {
        var localizer = new MutableLocalizer();
        var sink = new RecordingLogger<Category>();
        var logger = new LocalizedLogger<Category>(localizer, sink);
        var message = new LocalizeString(
            Scope,
            "FileRead",
            "Read {{ FileName }}.",
            ("FileName", "data.json"));

        localizer.Apply(
            CultureInfo.GetCultureInfo("zh-CN"),
            (message.LookupKey, "已读取 {{ FileName }}。"));
        logger.LogLocalizedInformation(message);

        localizer.Apply(
            CultureInfo.GetCultureInfo("en"),
            (message.LookupKey, "Read {{ FileName }}."));
        logger.LogLocalizedInformation(message);

        Assert.Collection(
            sink.Entries,
            entry =>
            {
                Assert.Equal("已读取 data.json。", entry.Message);
                Assert.Equal(LogLevel.Information, entry.Level);
                Assert.Equal("zh-CN", entry.Properties[LocalizedLogPropertyNames.Culture]);
                AssertStructuredProperties(entry.Properties);
            },
            entry =>
            {
                Assert.Equal("Read data.json.", entry.Message);
                Assert.Equal("en", entry.Properties[LocalizedLogPropertyNames.Culture]);
                AssertStructuredProperties(entry.Properties);
            });
    }

    [Fact]
    public void LogLocalized_WhenFormattingFails_UsesFallbackWithoutThrowing()
    {
        var sink = new RecordingLogger<Category>();
        var logger = new LocalizedLogger<Category>(new ThrowingLocalizer(), sink);
        var message = new LocalizeString(
            Scope,
            "Broken",
            "Embedded fallback",
            ("Value", 42));

        var exception = Record.Exception(() => logger.LogLocalizedError(message));

        Assert.Null(exception);
        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Embedded fallback", entry.Message);
        Assert.Equal(true, entry.Properties[LocalizedLogPropertyNames.FormatFailed]);
        Assert.Contains(
            typeof(FormatException).FullName!,
            Assert.IsType<string>(entry.Properties[LocalizedLogPropertyNames.FormatException]));
        Assert.Equal(42, entry.Properties["Value"]);
    }

    [Fact]
    public void StandardLoggerMethods_AreForwardedToTheInnerLogger()
    {
        var sink = new RecordingLogger<Category>();
        ILocalizedLogger<Category> logger = new LocalizedLogger<Category>(
            new MutableLocalizer(),
            sink);

        logger.LogInformation("Plain message");

        var entry = Assert.Single(sink.Entries);
        Assert.Equal("Plain message", entry.Message);
    }

    private static void AssertStructuredProperties(
        IReadOnlyDictionary<string, object?> properties)
    {
        Assert.Equal(Scope, properties[LocalizedLogPropertyNames.Scope]);
        Assert.Equal("FileRead", properties[LocalizedLogPropertyNames.Key]);
        Assert.Equal(
            Localizer.CombineScopeAndKey(Scope, "FileRead"),
            properties[LocalizedLogPropertyNames.LookupKey]);
        Assert.Equal(false, properties[LocalizedLogPropertyNames.FormatFailed]);
        Assert.Equal("data.json", properties["FileName"]);
        Assert.Equal("{LocalizedMessage}", properties["{OriginalFormat}"]);
    }

    private sealed class Category;

    private sealed class MutableLocalizer : Localizer
    {
        private CultureInfo culture = CultureInfo.InvariantCulture;
        private IReadOnlyDictionary<string, string> values =
            new Dictionary<string, string>();

        public override CultureInfo CurrentCulture => culture;

        public void Apply(
            CultureInfo culture,
            params (string Key, string Value)[] values)
        {
            this.culture = culture;
            this.values = values.ToDictionary(
                static value => value.Key,
                static value => value.Value,
                StringComparer.Ordinal);
        }

        public override bool TryQuery(string key, out string value)
            => values.TryGetValue(key, out value!);
    }

    private sealed class ThrowingLocalizer : Localizer
    {
        public override CultureInfo CurrentCulture => CultureInfo.GetCultureInfo("en");

        public override bool TryQuery(string key, out string value)
        {
            value = string.Empty;
            return false;
        }

        public override string Format(ILocalizeString str)
            => throw new FormatException("Invalid localized template.");
    }

    private sealed class RecordingLogger<TCategoryName> : ILogger<TCategoryName>
    {
        public List<Entry> Entries { get; } = [];

        public IDisposable BeginScope<TState>(TState state)
            => NoopScope.Instance;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            var properties = state is IEnumerable<KeyValuePair<string, object?>> values
                ? values.ToDictionary(static item => item.Key, static item => item.Value)
                : new Dictionary<string, object?>();
            Entries.Add(new Entry(
                logLevel,
                eventId,
                formatter(state, exception),
                exception,
                properties));
        }

        private sealed class NoopScope : IDisposable
        {
            public static NoopScope Instance { get; } = new();

            public void Dispose()
            {
            }
        }
    }

    private sealed record Entry(
        LogLevel Level,
        EventId EventId,
        string Message,
        Exception? Exception,
        IReadOnlyDictionary<string, object?> Properties);
}
