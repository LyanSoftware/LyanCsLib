using System.Globalization;
using Lytec.Common.Localization;
using Lytec.Common.Localization.Fluid;
using Xunit;

namespace Test.Common.Localization;

public sealed class FluidLocalizationTemplateRendererTest
{
    [Fact]
    public void Render_UsesNestedOrdinalDictionaryPaths()
    {
        var renderer = CreateRenderer();
        var variables = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase)
        {
            ["User"] = new Dictionary<string, object?>
            {
                ["Name"] = "Alice",
            },
        };

        var matching = renderer.Render(
            "Hello {{ User.Name }}.",
            variables,
            CultureInfo.InvariantCulture);
        var differentCase = renderer.Render(
            "Hello {{ user.Name }}.",
            variables,
            CultureInfo.InvariantCulture);

        Assert.Equal("Hello Alice.", matching);
        Assert.Equal("Hello .", differentCase);
    }

    [Fact]
    public void Render_DefaultFilterHandlesMissingVariables()
    {
        var result = CreateRenderer().Render(
            "{{ Name | default: \"Anonymous\" }}",
            null,
            CultureInfo.InvariantCulture);

        Assert.Equal("Anonymous", result);
    }

    [Fact]
    public void Render_FormatPreservesDotNetValueTypeAndCulture()
    {
        var renderer = CreateRenderer();
        var french = CultureInfo.GetCultureInfo("fr-FR");

        var hexadecimal = renderer.Render(
            "{{ Value | format: \"X2\" }}",
            new Dictionary<string, object?> { ["Value"] = 15 },
            french);
        var number = renderer.Render(
            "{{ Value | format: \"N2\" }}",
            new Dictionary<string, object?> { ["Value"] = 1234.5 },
            french);

        Assert.Equal("0F", hexadecimal);
        Assert.Equal(1234.5.ToString("N2", french), number);
    }

    [Fact]
    public void Render_DebugModeRejectsMissingVariables()
    {
        var exception = Assert.Throws<LocalizationTemplateException>(() =>
            CreateRenderer(debugMode: true).Render(
                "Hello {{ Name }}.",
                null,
                CultureInfo.InvariantCulture));

        Assert.Contains("Name", exception.Message);
    }

    [Theory]
    [InlineData("{% if Value %}yes{% endif %}")]
    [InlineData("{{ Value | upcase }}")]
    [InlineData("{{ Value[0] }}")]
    [InlineData("{{ \"literal\" }}")]
    public void Render_RejectsSyntaxOutsideThePortableSubset(string template)
    {
        Assert.Throws<LocalizationTemplateException>(() =>
            CreateRenderer().Render(
                template,
                new Dictionary<string, object?> { ["Value"] = "value" },
                CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Render_DoesNotReflectClrProperties()
    {
        var result = CreateRenderer().Render(
            "{{ User.Name }}",
            new Dictionary<string, object?>
            {
                ["User"] = new TestUser("Alice"),
            },
            CultureInfo.InvariantCulture);

        Assert.Equal(string.Empty, result);
    }

    private static FluidLocalizationTemplateRenderer CreateRenderer(bool debugMode = false)
        => new(new LocalizationTemplateOptions { DebugMode = debugMode });

    private sealed record TestUser(string Name);
}
