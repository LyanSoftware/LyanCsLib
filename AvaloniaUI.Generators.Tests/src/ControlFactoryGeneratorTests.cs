using System.Collections.Immutable;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Declarative;
using Lytec.AvaloniaUI.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lytec.AvaloniaUI.Generators.Tests;

public sealed class ControlFactoryGeneratorTests
{
    [Fact]
    public void GeneratesOfficialControlFactoriesAndDeclarativeOverloads()
    {
        var result = RunGenerator(Target(""));

        AssertNoErrors(result);
        var source = result.FactorySource;
        Assert.Contains("MenuItem(object? header", source);
        Assert.Contains("MenuItem(global::Avalonia.Data.BindingBase binding", source);
        Assert.Contains("MenuItem<TViewModel, TValue>", source);
        Assert.Contains("Grid(params global::Avalonia.Controls.Control[] children)", source);
        Assert.Contains("TextBlock(string? text", source);
        Assert.DoesNotContain("RowDefinition RowDefinition(", source);
        Assert.DoesNotContain("ContextMenu ContextMenu(", source);
    }

    [Fact]
    public void GeneratesExplicitThirdPartyControlWithAliasAndPrimaryProperty()
    {
        var result = RunGenerator(Target("""
            [IncludeControlFactory(
                typeof(TestApp.FancyControl),
                Alias = "Fancy",
                PrimaryProperty = nameof(TestApp.FancyControl.Caption))]
            """) + """

            namespace TestApp
            {
                public sealed class FancyControl : global::Avalonia.Controls.Control
                {
                    public static readonly global::Avalonia.StyledProperty<string?> CaptionProperty =
                        global::Avalonia.AvaloniaProperty.Register<FancyControl, string?>(nameof(Caption));

                    public string? Caption
                    {
                        get => GetValue(CaptionProperty);
                        set => SetValue(CaptionProperty, value);
                    }
                }
            }
            """);

        AssertNoErrors(result);
        Assert.Contains("Fancy(string? caption", result.FactorySource);
        Assert.Contains("Fancy<TViewModel, TValue>", result.FactorySource);
        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Id == "LYAVGEN008");
    }

    [Fact]
    public void WarnsWhenIncludedControlHasNoPrimaryProperty()
    {
        var result = RunGenerator(Target("""
            [IncludeControlFactory(typeof(TestApp.EmptyControl))]
            """) + """

            namespace TestApp
            {
                public sealed class EmptyControl : global::Avalonia.Controls.Control
                {
                }
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic =>
            diagnostic.Id == "LYAVGEN008" &&
            diagnostic.Severity == DiagnosticSeverity.Warning);
        Assert.Contains("EmptyControl()", result.FactorySource);
    }

    [Fact]
    public void ReportsNonPartialTarget()
    {
        var result = RunGenerator("""
            using Lytec.AvaloniaUI.Generators;

            namespace TestApp
            {
                [GenerateControlFactories]
                public abstract class TestViewBase
                    : global::Avalonia.Markup.Declarative.ViewBase
                {
                    protected TestViewBase()
                        : base(global::Avalonia.Markup.Declarative.ViewInitializationStrategy.Lazy)
                    {
                    }
                }
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYAVGEN001");
    }

    [Fact]
    public void ReportsInvalidExplicitPrimaryProperty()
    {
        var result = RunGenerator(Target("""
            [IncludeControlFactory(
                typeof(TestApp.EmptyControl),
                PrimaryProperty = "Missing")]
            """) + """

            namespace TestApp
            {
                public sealed class EmptyControl : global::Avalonia.Controls.Control
                {
                }
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYAVGEN005");
    }

    [Fact]
    public void ReportsAliasCollisionWithOfficialControl()
    {
        var result = RunGenerator(Target("""
            [IncludeControlFactory(typeof(TestApp.FancyControl), Alias = "Button")]
            """) + """

            namespace TestApp
            {
                public sealed class FancyControl : global::Avalonia.Controls.Control
                {
                }
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYAVGEN006");
    }

    [Fact]
    public void ReportsTargetThatDoesNotDeriveFromViewBase()
    {
        var result = RunGenerator("""
            using Lytec.AvaloniaUI.Generators;

            namespace TestApp
            {
                [GenerateControlFactories]
                public abstract partial class InvalidTarget
                {
                }
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYAVGEN002");
    }

    [Fact]
    public void ExistingFactoryForTheSameControlTakesPriority()
    {
        var result = RunGenerator("""
            using Lytec.AvaloniaUI.Generators;

            namespace TestApp
            {
                [GenerateControlFactories]
                public abstract partial class TestViewBase
                    : global::Avalonia.Markup.Declarative.ViewBase
                {
                    protected TestViewBase()
                        : base(global::Avalonia.Markup.Declarative.ViewInitializationStrategy.Lazy)
                    {
                    }

                    public static global::Avalonia.Controls.MenuItem MenuItem() => new();
                }
            }
            """);

        AssertNoErrors(result);
        Assert.DoesNotContain("MenuItem() =>", result.FactorySource);
        Assert.Contains("MenuItem(object? header", result.FactorySource);
    }

    private static string Target(string attributes) => $$"""
        using Lytec.AvaloniaUI.Generators;

        namespace TestApp
        {
            [GenerateControlFactories]
            {{attributes}}
            public abstract partial class TestViewBase
                : global::Avalonia.Markup.Declarative.ViewBase
            {
                protected TestViewBase()
                    : base(global::Avalonia.Markup.Declarative.ViewInitializationStrategy.Lazy)
                {
                }
            }
        }
        """;

    private static GeneratorResult RunGenerator(string source)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
        var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            GetMetadataReferences(),
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                nullableContextOptions: NullableContextOptions.Enable));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new ControlFactoryGenerator().AsSourceGenerator()],
            parseOptions: parseOptions);
        driver = driver.RunGeneratorsAndUpdateCompilation(
            compilation,
            out var outputCompilation,
            out var generatorDiagnostics);

        var runResult = driver.GetRunResult();
        var generatedSources = runResult.Results
            .SelectMany(static result => result.GeneratedSources)
            .ToArray();
        var factorySource = generatedSources
            .FirstOrDefault(static generated =>
                generated.HintName.EndsWith(".ControlFactories.g.cs", StringComparison.Ordinal))
            .SourceText?
            .ToString() ?? string.Empty;
        var diagnostics = generatorDiagnostics
            .Concat(runResult.Diagnostics)
            .Concat(outputCompilation.GetDiagnostics())
            .Distinct(DiagnosticComparer.Instance)
            .ToImmutableArray();

        return new GeneratorResult(factorySource, diagnostics);
    }

    private static IEnumerable<MetadataReference> GetMetadataReferences()
    {
        var paths = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string platformAssemblies)
        {
            foreach (var path in platformAssemblies.Split(Path.PathSeparator))
                paths.Add(path);
        }

        paths.Add(typeof(AvaloniaObject).Assembly.Location);
        paths.Add(typeof(Control).Assembly.Location);
        paths.Add(typeof(ViewBase).Assembly.Location);

        return paths.Select(static path => MetadataReference.CreateFromFile(path));
    }

    private static void AssertNoErrors(GeneratorResult result)
    {
        var errors = result.Diagnostics
            .Where(static diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)
            .ToArray();
        Assert.True(errors.Length == 0, string.Join(Environment.NewLine, errors.Select(static x => x.ToString())));
    }

    private sealed record GeneratorResult(
        string FactorySource,
        ImmutableArray<Diagnostic> Diagnostics);

    private sealed class DiagnosticComparer : IEqualityComparer<Diagnostic>
    {
        public static DiagnosticComparer Instance { get; } = new();

        public bool Equals(Diagnostic? x, Diagnostic? y) =>
            x?.Id == y?.Id &&
            x?.Location.SourceSpan == y?.Location.SourceSpan &&
            x?.GetMessage() == y?.GetMessage();

        public int GetHashCode(Diagnostic obj) => HashCode.Combine(
            obj.Id,
            obj.Location.SourceSpan,
            obj.GetMessage());
    }
}
