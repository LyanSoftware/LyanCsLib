using Lytec.Common.Generators;
using Lytec.Common.Serialization;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;

namespace Lytec.Common.Generators.Tests;

public sealed class FixedBinarySerializationGeneratorTests
{
    [Theory]
    [InlineData("[FixedBinarySerializable(BinaryLayoutKind.Sequential)] public partial struct Value<T> { public int Number; }", "LYBIN001")]
    [InlineData("[FixedBinarySerializable(BinaryLayoutKind.Sequential)] public struct Value { public int Number; }", "LYBIN001")]
    [InlineData("[FixedBinarySerializable((BinaryLayoutKind)99)] public partial struct Value { public int Number; }", "LYBIN003")]
    [InlineData("[FixedBinarySerializable(BinaryLayoutKind.Sequential)] public partial struct Value { public byte[] Data; }", "LYBIN002")]
    public void ReportsUnsupportedDeclarations(string declaration, string diagnosticId)
    {
        var result = Run(declaration);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == diagnosticId);
    }

    [Fact]
    public void DoesNotGenerateForUnannotatedInterfaceImplementation()
    {
        var result = Run("public partial struct Value : IBinarySerializable { public int Number; }");

        Assert.Empty(result.GeneratedSources);
        Assert.Empty(result.Diagnostics);
    }

    [Fact]
    public void GeneratesInterfaceAndPublicCodecSurface()
    {
        var result = Run(
            "[FixedBinarySerializable(BinaryLayoutKind.Sequential)] public partial struct Value { public ushort Number; }",
            assertCompilation: true);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("IBinarySerializable", generated);
        Assert.Contains("BinaryCodec", generated);
        Assert.Contains("GetBinaryCodec", generated);
        Assert.Contains("TryDeserialize", generated);
    }

    [Fact]
    public void IgnoresStructLayoutAndMarshalAs()
    {
        var result = Run("""
            [StructLayout(LayoutKind.Explicit)]
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [FieldOffset(8)]
                [MarshalAs(UnmanagedType.I4)]
                public byte Number;
            }
            """,
            assertCompilation: true);

        Assert.Empty(result.Diagnostics);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("SerializedSize => 1;", generated);
    }

    [Theory]
    [InlineData("sbyte", 2)]
    [InlineData("byte", 2)]
    [InlineData("short", 4)]
    [InlineData("ushort", 4)]
    [InlineData("int", 8)]
    [InlineData("uint", 8)]
    [InlineData("long", 16)]
    [InlineData("ulong", 16)]
    [InlineData("float", 8)]
    [InlineData("double", 16)]
    public void InfersFixedArrayElementTypeForPrimitives(string elementType, int serializedSize)
    {
        var result = Run($$"""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2)]
                public {{elementType}}[] Data;
            }
            """,
            assertCompilation: true);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains($"SerializedSize => {serializedSize};", generated);
    }

    [Fact]
    public void InfersFixedArrayElementTypeFromEnumUnderlyingType()
    {
        var result = Run("""
            public enum Code : ushort { None }

            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2)]
                public Code[] Data;
            }
            """,
            assertCompilation: true);

        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("SerializedSize => 4;", generated);
    }

    [Theory]
    [InlineData("struct")]
    [InlineData("class")]
    public void InfersInlineArrayElementsFromGeneratedContract(string typeKind)
    {
        var result = Run($$"""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial {{typeKind}} Item
            {
                public ushort Number;
            }

            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2)]
                public Item[] Data;
            }
            """,
            assertCompilation: true);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Equal(2, result.GeneratedSources.Length);
    }

    [Fact]
    public void RequiresExplicitDataTypeForBooleanElements()
    {
        var result = Run("""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2)]
                public bool[] Data;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN002");
    }

    [Theory]
    [InlineData("Boolean8", 2)]
    [InlineData("Boolean16", 4)]
    [InlineData("Boolean32", 8)]
    public void SupportsBooleanDataTypes(string dataType, int serializedSize)
    {
        var result = Run($$"""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2, ElementType = BinaryDataType.{{dataType}})]
                public bool[] Data;
            }
            """,
            assertCompilation: true);

        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains($"SerializedSize => {serializedSize};", generated);
    }

    [Fact]
    public void UsesExplicitElementTypeInsteadOfInferredRepresentation()
    {
        var result = Run("""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2, ElementType = BinaryDataType.Int16)]
                public byte[] Data;
            }
            """,
            assertCompilation: true);

        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("SerializedSize => 4;", generated);
    }

    [Fact]
    public void RejectsIncompatibleExplicitElementType()
    {
        var result = Run("""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFixedArray(2, ElementType = BinaryDataType.Int32)]
                public float[] Data;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN002");
    }

    [Fact]
    public void ExplicitOptInTreatsBinaryFieldOffsetAsInclusion()
    {
        var result = Run("""
            [FixedBinarySerializable(
                BinaryLayoutKind.Explicit,
                MemberInclusion = BinaryMemberInclusion.OptIn,
                Size = 4)]
            public partial struct Value
            {
                public byte Ignored;

                [BinaryFieldOffset(2)]
                public ushort Number;
            }
            """,
            assertCompilation: true);

        var generated = Assert.Single(result.GeneratedSources).SourceText.ToString();
        Assert.Contains("SerializedSize => 4;", generated);
        Assert.DoesNotContain("this.Ignored", generated);
    }

    [Fact]
    public void RejectsBinaryFieldOffsetInSequentialLayout()
    {
        var result = Run("""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                [BinaryFieldOffset(2)]
                public ushort Number;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN002");
    }

    [Fact]
    public void RejectsExplicitSizeSmallerThanMembers()
    {
        var result = Run("""
            [FixedBinarySerializable(BinaryLayoutKind.Sequential, Size = 1)]
            public partial struct Value
            {
                public ushort Number;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN003");
    }

    [Theory]
    [InlineData("")]
    [InlineData("[BinaryFixedArray(2)]")]
    public void SupportsManualFixedBinaryContracts(string arrayAttribute)
    {
        var memberType = arrayAttribute.Length == 0 ? "Item" : "Item[]";
        var result = Run($$"""
            [FixedBinaryContract(2, typeof(ItemCodecProvider))]
            public struct Item : IBinarySerializable
            {
                public int SerializedSize => 2;
                public byte[] Serialize(Endian? endian = null) => throw new NotImplementedException();
                public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? endian = null) => throw new NotImplementedException();
            }

            public sealed class ItemCodecProvider : IFixedBinaryCodecProvider<Item>
            {
                public IFixedBinaryCodec<Item> GetBinaryCodec(Endian? endian = null) => throw new NotImplementedException();
            }

            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                {{arrayAttribute}}
                public {{memberType}} Data;
            }
            """,
            assertCompilation: true);

        Assert.DoesNotContain(result.Diagnostics, diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        Assert.Single(result.GeneratedSources);
    }

    [Fact]
    public void RejectsInvalidManualCodecProvider()
    {
        var result = Run("""
            [FixedBinaryContract(2, typeof(object))]
            public struct Item : IBinarySerializable
            {
                public int SerializedSize => 2;
                public byte[] Serialize(Endian? endian = null) => throw new NotImplementedException();
                public OperationStatus TrySerialize(Span<byte> destination, out int written, Endian? endian = null) => throw new NotImplementedException();
            }

            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                public Item Data;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN002");
    }

    [Fact]
    public void RejectsAutomaticAndManualContractOnSameType()
    {
        var result = Run("""
            [FixedBinaryContract(2, typeof(object))]
            [FixedBinarySerializable(BinaryLayoutKind.Sequential)]
            public partial struct Value
            {
                public ushort Number;
            }
            """);

        Assert.Contains(result.Diagnostics, diagnostic => diagnostic.Id == "LYBIN001");
    }

    private static GeneratorRunResult Run(string declaration, bool assertCompilation = false)
    {
        var source = $$"""
            using System;
            using System.Buffers;
            using System.Runtime.InteropServices;
            using Lytec.Common.Data;
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
        driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);
        if (assertCompilation)
            Assert.DoesNotContain(
                outputCompilation.GetDiagnostics(),
                diagnostic => diagnostic.Severity == DiagnosticSeverity.Error);
        return Assert.Single(driver.GetRunResult().Results);
    }
}
