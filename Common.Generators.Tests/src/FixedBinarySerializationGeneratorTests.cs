using Lytec.Common.Generators;
using Lytec.Common.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lytec.Common.Generators.Tests;

public sealed class FixedBinarySerializationGeneratorTests
{
    [Theory]
    [InlineData("public partial struct Value<T> : IBinarySerializable { public int Number; }", "LYBIN001")]
    [InlineData("public partial struct Value : IBinarySerializable { public byte[] Data; }", "LYBIN002")]
    [InlineData("[StructLayout(LayoutKind.Auto)] public partial class Value : IBinarySerializable { public int Number; }", "LYBIN003")]
    public void ReportsUnsupportedDeclarations(string declaration, string diagnosticId)
    {
        var result = Run(declaration);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == diagnosticId);
    }

    [Fact]
    public void ReportsSuppressibleCharSetWarning()
    {
        var result = Run("[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)] public partial struct Value : IBinarySerializable { public int Number; }");

        var warning = Assert.Single(result.Diagnostics.Where(diagnostic => diagnostic.Id == "LYBIN101"));
        Assert.Equal(DiagnosticSeverity.Warning, warning.Severity);
    }

    [Fact]
    public void GeneratesPublicCodecSurface()
    {
        var result = Run("[StructLayout(LayoutKind.Sequential, Pack = 1)] public partial struct Value : IBinarySerializable { public ushort Number; }");

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("BinaryCodec", generated);
        Assert.Contains("GetBinaryCodec", generated);
        Assert.Contains("TryDeserialize", generated);
    }

    private static GeneratorRunResult Run(string declaration)
    {
        var source = $$"""
            using System.Runtime.InteropServices;
            using Lytec.Common.Serialization;

            namespace Samples;

            {{declaration}}
            """;
        var syntaxTree = CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview));
        var references = ((string)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES")!)
            .Split(Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .Append(MetadataReference.CreateFromFile(typeof(IBinarySerializable).Assembly.Location));
        var compilation = CSharpCompilation.Create(
            "GeneratorTests",
            [syntaxTree],
            references,
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        GeneratorDriver driver = CSharpGeneratorDriver.Create(
            [new FixedBinarySerializationGenerator().AsSourceGenerator()],
            parseOptions: (CSharpParseOptions)syntaxTree.Options);
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out _, out _);
        return Assert.Single(driver.GetRunResult().Results);
    }
}
