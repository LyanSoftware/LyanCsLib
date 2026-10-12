using System.Globalization;
using Lytec.Common.Localization;
using Xunit;

namespace Test.Common.Localization;

public sealed class LocalizerTemplateFailureTest
{
    [Fact]
    public void Format_NormalModeTriesDefaultMessageAfterTranslationFailure()
    {
        var renderer = new SelectiveRenderer("broken translation");
        var options = new LocalizationTemplateOptions();
        var localizer = new TestLocalizer(renderer, options, "broken translation");

        var result = localizer.Format(new LocalizeString(
            "Key",
            DefaultMessage: "valid default"));

        Assert.Equal("valid default", result);
    }

    [Fact]
    public void Format_DebugModeReturnsBrokenTranslationWithoutTryingDefaultMessage()
    {
        var renderer = new SelectiveRenderer("broken translation");
        var options = new LocalizationTemplateOptions { DebugMode = true };
        var localizer = new TestLocalizer(renderer, options, "broken translation");

        var result = localizer.Format(new LocalizeString(
            "Key",
            DefaultMessage: "valid default"));

        Assert.Equal("broken translation", result);
        Assert.Equal(new[] { "broken translation" }, renderer.RenderedTemplates);
    }

    [Fact]
    public void Format_NormalModeReturnsRawDefaultWhenBothTemplatesFail()
    {
        var renderer = new SelectiveRenderer("broken translation", "broken default");
        var options = new LocalizationTemplateOptions();
        var localizer = new TestLocalizer(renderer, options, "broken translation");

        var result = localizer.Format(new LocalizeString(
            "Key",
            DefaultMessage: "broken default"));

        Assert.Equal("broken default", result);
    }

    private sealed class TestLocalizer(
        ILocalizationTemplateRenderer renderer,
        LocalizationTemplateOptions options,
        string translation)
        : Localizer(renderer, options)
    {
        public override CultureInfo CurrentCulture => CultureInfo.InvariantCulture;

        public override bool TryQuery(string key, out string value)
        {
            value = translation;
            return true;
        }
    }

    private sealed class SelectiveRenderer(params string[] failures)
        : ILocalizationTemplateRenderer
    {
        private readonly HashSet<string> failures = failures.ToHashSet(StringComparer.Ordinal);

        public List<string> RenderedTemplates { get; } = [];

        public string Render(
            string template,
            IReadOnlyDictionary<string, object?>? variables,
            CultureInfo culture)
        {
            RenderedTemplates.Add(template);
            if (failures.Contains(template))
                throw new LocalizationTemplateException("Rejected for testing.");
            return template;
        }
    }
}
