using System.Globalization;
using System.Text;
using Lytec.Analyzer;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lytec.Common.Generators;

[Generator]
public sealed class GenerateHashAlgorithmExtensionsGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor FactoryMustBeStatic = new(
        id: "LYTEC_COMMON_HASH_001",
        title: "Invalid hash algorithm factory",
        messageFormat: "Method '{0}' marked with [GenerateHashAlgorithmExtensions] must be a static method",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ContainerMustBePartialStaticClass = new(
        id: "LYTEC_COMMON_HASH_002",
        title: "Invalid hash algorithm factory container",
        messageFormat: "Method '{0}' marked with [GenerateHashAlgorithmExtensions] must be declared in a partial static class",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor ContainingTypeMustBePartial = new(
        id: "LYTEC_COMMON_HASH_003",
        title: "Invalid containing type",
        messageFormat: "Containing type '{0}' must be partial so hash algorithm extensions can be generated",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor FactoryMustReturnHashAlgorithm = new(
        id: "LYTEC_COMMON_HASH_004",
        title: "Invalid hash algorithm factory return type",
        messageFormat: "Method '{0}' marked with [GenerateHashAlgorithmExtensions] must return System.Security.Cryptography.HashAlgorithm or a derived type",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor InvalidConverter = new(
        id: "LYTEC_COMMON_HASH_005",
        title: "Invalid hash result converter",
        messageFormat: "Converter '{0}' for method '{1}' must be a unique static method in the same class with signature T {0}(byte[] bytes)",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly SymbolDisplayFormat TypeDisplayFormat =
        SymbolDisplayFormat.FullyQualifiedFormat.WithMiscellaneousOptions(
            SymbolDisplayFormat.FullyQualifiedFormat.MiscellaneousOptions |
            SymbolDisplayMiscellaneousOptions.UseSpecialTypes |
            SymbolDisplayMiscellaneousOptions.IncludeNullableReferenceTypeModifier);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var methods = context.SyntaxProvider.ForAttr<GenerateHashAlgorithmExtensionsAttribute>(
            node => node is MethodDeclarationSyntax);

        context.RegisterSourceOutput(
            context.CompilationProvider.Combine(methods),
            static (sourceProductionContext, source) =>
                Generate(sourceProductionContext, source.Left, source.Right));
    }

    private static void Generate(
        SourceProductionContext context,
        Compilation compilation,
        IEnumerable<SymbolWithAttrInfo> infos)
    {
        var hashAlgorithmType = compilation.GetTypeByMetadataName(
            "System.Security.Cryptography.HashAlgorithm");
        if (hashAlgorithmType is null)
            return;

        var targetsByContainer =
            new Dictionary<INamedTypeSymbol, List<GenerationTarget>>(SymbolEqualityComparer.Default);

        foreach (var info in infos)
        {
            if (info.Symbol is not IMethodSymbol factory || info.Attrs.Length == 0)
                continue;

            var attribute = info.Attrs[0];
            var location = GetDiagnosticLocation(attribute, factory, context.CancellationToken);

            if (factory.MethodKind != MethodKind.Ordinary || !factory.IsStatic)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FactoryMustBeStatic,
                    location,
                    factory.Name));
                continue;
            }

            var container = factory.ContainingType;
            if (container.TypeKind != TypeKind.Class ||
                !container.IsStatic ||
                !IsPartial(container))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContainerMustBePartialStaticClass,
                    location,
                    factory.Name));
                continue;
            }

            var nonPartialContainingType = GetContainingTypes(container)
                .FirstOrDefault(type => !IsPartial(type));
            if (nonPartialContainingType is not null)
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    ContainingTypeMustBePartial,
                    location,
                    nonPartialContainingType.ToDisplayString(
                        SymbolDisplayFormat.MinimallyQualifiedFormat)));
                continue;
            }

            if (!IsHashAlgorithm(factory.ReturnType, hashAlgorithmType))
            {
                context.ReportDiagnostic(Diagnostic.Create(
                    FactoryMustReturnHashAlgorithm,
                    location,
                    factory.Name));
                continue;
            }

            var converterName = GetConverterName(attribute);
            IMethodSymbol? converter = null;
            ITypeSymbol resultType = compilation.CreateArrayTypeSymbol(
                compilation.GetSpecialType(SpecialType.System_Byte));

            if (converterName is not null)
            {
                var converters = container.GetMembers(converterName)
                    .OfType<IMethodSymbol>()
                    .Where(IsValidConverter)
                    .ToArray();

                if (converters.Length != 1)
                {
                    context.ReportDiagnostic(Diagnostic.Create(
                        InvalidConverter,
                        location,
                        converterName,
                        factory.Name));
                    continue;
                }

                converter = converters[0];
                resultType = converter.ReturnType;
            }

            if (!targetsByContainer.TryGetValue(container, out var targets))
            {
                targets = new List<GenerationTarget>();
                targetsByContainer.Add(container, targets);
            }

            targets.Add(new GenerationTarget(factory, converter, resultType));
        }

        foreach (var pair in targetsByContainer)
        {
            var container = pair.Key;
            var targets = pair.Value
                .OrderBy(target => GetSourceOrder(target.Factory))
                .ThenBy(target => target.Factory.Name, StringComparer.Ordinal)
                .ToArray();
            var hintName = $"{container.GetFiltedMetadataName()}.HashAlgorithmExtensions.g.cs";

            context.AddSource(
                hintName,
                SourceText.From(GenerateSource(container, targets), Encoding.UTF8));
        }
    }

    private static string GenerateSource(
        INamedTypeSymbol container,
        IReadOnlyList<GenerationTarget> targets)
    {
        var sb = new StringBuilder();
        var typeChain = GetTypeChain(container);

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");
        sb.AppendLine("using System.Security.Cryptography;");
        sb.AppendLine();

        if (!container.ContainingNamespace.IsGlobalNamespace)
        {
            sb.Append("namespace ")
                .Append(container.ContainingNamespace.ToDisplayString())
                .AppendLine();
            sb.AppendLine("{");
        }

        foreach (var type in typeChain)
        {
            AppendIndent(sb, GetIndent(type, typeChain, container));
            sb.Append(GetTypeDeclaration(type)).AppendLine();
            AppendTypeConstraints(
                sb,
                type,
                GetIndent(type, typeChain, container));
            AppendIndent(sb, GetIndent(type, typeChain, container));
            sb.AppendLine("{");
        }

        var memberIndent = typeChain.Count + (container.ContainingNamespace.IsGlobalNamespace ? 0 : 1);
        foreach (var target in targets)
            AppendExtensions(sb, target, memberIndent);

        for (var i = typeChain.Count - 1; i >= 0; i--)
        {
            var indent = i + (container.ContainingNamespace.IsGlobalNamespace ? 0 : 1);
            AppendIndent(sb, indent);
            sb.AppendLine("}");
        }

        if (!container.ContainingNamespace.IsGlobalNamespace)
            sb.AppendLine("}");

        sb.AppendLine("#nullable restore");

        return CSharpSyntaxTree.ParseText(sb.ToString())
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();
    }

    private static void AppendExtensions(
        StringBuilder sb,
        GenerationTarget target,
        int indent)
    {
        var factoryParameters = target.Factory.Parameters;
        var bytesName = GetUniqueParameterName("bytes", factoryParameters);
        var streamName = GetUniqueParameterName("stream", factoryParameters);
        var countName = GetUniqueParameterName("count", factoryParameters);
        var offsetName = GetUniqueParameterName("offset", factoryParameters, countName);

        AppendExtension(
            sb,
            target,
            indent,
            $"this byte[] {bytesName}",
            $"{bytesName}, 0, {bytesName}.Length");
        AppendExtension(
            sb,
            target,
            indent,
            $"this byte[] {bytesName}, int {countName}",
            $"{bytesName}, 0, {countName}");
        AppendExtension(
            sb,
            target,
            indent,
            $"this byte[] {bytesName}, int {offsetName}, int {countName}",
            $"{bytesName}, {offsetName}, {countName}");
        AppendExtension(
            sb,
            target,
            indent,
            $"this global::System.IO.Stream {streamName}",
            streamName);
        AppendExtension(
            sb,
            target,
            indent,
            $"this global::System.Collections.Generic.IEnumerable<byte> {bytesName}",
            bytesName);
    }

    private static void AppendExtension(
        StringBuilder sb,
        GenerationTarget target,
        int indent,
        string sourceParameter,
        string computeHashArguments)
    {
        var factory = target.Factory;
        var parameters = new List<string> { sourceParameter };
        parameters.AddRange(factory.Parameters.Select(FormatParameter));

        AppendIndent(sb, indent);
        sb.Append("public static ")
            .Append(GetTypeName(target.ResultType))
            .Append(' ')
            .Append(EscapeIdentifier(factory.Name))
            .Append(GetTypeParameterList(factory.TypeParameters))
            .Append('(')
            .Append(string.Join(", ", parameters))
            .AppendLine(")");

        AppendMethodConstraints(sb, factory.TypeParameters, indent + 1);

        var computeHash = new StringBuilder()
            .Append(GetFactoryInvocation(factory))
            .Append(".ComputeHash(")
            .Append(computeHashArguments)
            .Append(')')
            .ToString();

        if (target.Converter is not null)
        {
            computeHash = EscapeIdentifier(target.Converter.Name) +
                "(" + computeHash + ")";
        }

        AppendIndent(sb, indent + 1);
        sb.Append("=> ").Append(computeHash).AppendLine(";");
        sb.AppendLine();
    }

    private static string GetFactoryInvocation(IMethodSymbol factory)
    {
        var arguments = factory.Parameters.Select(parameter =>
        {
            var modifier = parameter.RefKind switch
            {
                RefKind.Ref => "ref ",
                RefKind.Out => "out ",
                RefKind.In => "in ",
                _ => string.Empty
            };
            return modifier + EscapeIdentifier(parameter.Name);
        });

        return EscapeIdentifier(factory.Name) +
            GetTypeParameterList(factory.TypeParameters) +
            "(" + string.Join(", ", arguments) + ")";
    }

    private static string FormatParameter(IParameterSymbol parameter)
    {
        var sb = new StringBuilder();
        if (parameter.IsParams)
            sb.Append("params ");

        switch (parameter.RefKind)
        {
            case RefKind.Ref:
                sb.Append("ref ");
                break;
            case RefKind.Out:
                sb.Append("out ");
                break;
            case RefKind.In:
                sb.Append("in ");
                break;
        }

        sb.Append(GetTypeName(parameter.Type))
            .Append(' ')
            .Append(EscapeIdentifier(parameter.Name));

        if (parameter.HasExplicitDefaultValue)
        {
            sb.Append(" = ")
                .Append(GetDefaultValue(parameter));
        }

        return sb.ToString();
    }

    private static string GetDefaultValue(IParameterSymbol parameter)
    {
        foreach (var syntaxReference in parameter.ContainingSymbol.DeclaringSyntaxReferences)
        {
            if (syntaxReference.GetSyntax() is not MethodDeclarationSyntax declaration ||
                parameter.Ordinal >= declaration.ParameterList.Parameters.Count)
            {
                continue;
            }

            var defaultValue = declaration.ParameterList.Parameters[parameter.Ordinal].Default;
            if (defaultValue is not null)
                return defaultValue.Value.WithoutTrivia().ToFullString();
        }

        if (parameter.ExplicitDefaultValue is null)
            return "null";

        if (parameter.Type.TypeKind == TypeKind.Enum)
        {
            return "(" + GetTypeName(parameter.Type) + ")" +
                Convert.ToString(parameter.ExplicitDefaultValue, CultureInfo.InvariantCulture);
        }

        return SymbolDisplay.FormatPrimitive(
            parameter.ExplicitDefaultValue,
            quoteStrings: true,
            useHexadecimalNumbers: false);
    }

    private static void AppendMethodConstraints(
        StringBuilder sb,
        IEnumerable<ITypeParameterSymbol> typeParameters,
        int indent)
    {
        foreach (var typeParameter in typeParameters)
        {
            var constraints = GetConstraints(typeParameter).ToArray();
            if (constraints.Length == 0)
                continue;

            AppendIndent(sb, indent);
            sb.Append("where ")
                .Append(EscapeIdentifier(typeParameter.Name))
                .Append(" : ")
                .Append(string.Join(", ", constraints))
                .AppendLine();
        }
    }

    private static void AppendTypeConstraints(
        StringBuilder sb,
        INamedTypeSymbol type,
        int indent)
    {
        AppendMethodConstraints(sb, type.TypeParameters, indent + 1);
    }

    private static IEnumerable<string> GetConstraints(ITypeParameterSymbol typeParameter)
    {
        if (typeParameter.HasNotNullConstraint)
            yield return "notnull";
        else if (typeParameter.HasReferenceTypeConstraint)
            yield return "class";

        if (typeParameter.HasUnmanagedTypeConstraint)
            yield return "unmanaged";
        else if (typeParameter.HasValueTypeConstraint)
            yield return "struct";

        foreach (var constraintType in typeParameter.ConstraintTypes)
            yield return GetTypeName(constraintType);

        if (typeParameter.HasConstructorConstraint)
            yield return "new()";
    }

    private static string GetTypeDeclaration(INamedTypeSymbol type)
    {
        var staticModifier = type.IsStatic ? "static " : string.Empty;
        return staticModifier + "partial " + GetTypeKeyword(type) + " " +
            EscapeIdentifier(type.Name) + GetTypeParameterList(type.TypeParameters);
    }

    private static string GetTypeKeyword(INamedTypeSymbol type)
    {
        if (type.IsRecord)
            return type.IsValueType ? "record struct" : "record";

        return type.TypeKind switch
        {
            TypeKind.Class => "class",
            TypeKind.Struct => "struct",
            TypeKind.Interface => "interface",
            _ => "class"
        };
    }

    private static string GetTypeParameterList(
        IEnumerable<ITypeParameterSymbol> typeParameters)
    {
        var names = typeParameters
            .Select(typeParameter => EscapeIdentifier(typeParameter.Name))
            .ToArray();
        return names.Length == 0 ? string.Empty : "<" + string.Join(", ", names) + ">";
    }

    private static string GetTypeName(ITypeSymbol type) =>
        type.ToDisplayString(TypeDisplayFormat);

    private static string EscapeIdentifier(string name)
    {
        return SyntaxFacts.GetKeywordKind(name) != SyntaxKind.None
            ? "@" + name
            : name;
    }

    private static string GetUniqueParameterName(
        string preferredName,
        IEnumerable<IParameterSymbol> factoryParameters,
        params string[] additionalNames)
    {
        var names = new HashSet<string>(
            factoryParameters.Select(parameter => parameter.Name)
                .Concat(additionalNames),
            StringComparer.Ordinal);
        var candidate = preferredName;
        var suffix = 1;

        while (names.Contains(candidate))
            candidate = preferredName + suffix++;

        return EscapeIdentifier(candidate);
    }

    private static bool IsValidConverter(IMethodSymbol method)
    {
        return method.MethodKind == MethodKind.Ordinary &&
               method.IsStatic &&
               !method.IsGenericMethod &&
               !method.ReturnsVoid &&
               !method.ReturnsByRef &&
               !method.ReturnsByRefReadonly &&
               method.Parameters.Length == 1 &&
               method.Parameters[0].RefKind == RefKind.None &&
               IsByteArray(method.Parameters[0].Type);
    }

    private static bool IsByteArray(ITypeSymbol type)
    {
        return type is IArrayTypeSymbol
        {
            Rank: 1,
            ElementType.SpecialType: SpecialType.System_Byte
        };
    }

    private static string? GetConverterName(AttributeData attribute)
    {
        var namedValue = attribute.NamedArguments
            .FirstOrDefault(argument =>
                argument.Key == nameof(
                    GenerateHashAlgorithmExtensionsAttribute.ConvertResultFuncName))
            .Value;
        if (namedValue.Value is string name)
            return name;

        if (attribute.ConstructorArguments.Length > 0 &&
            attribute.ConstructorArguments[0].Value is string constructorName)
        {
            return constructorName;
        }

        return null;
    }

    private static bool IsHashAlgorithm(
        ITypeSymbol type,
        INamedTypeSymbol hashAlgorithmType)
    {
        if (type is ITypeParameterSymbol typeParameter)
        {
            return typeParameter.ConstraintTypes.Any(
                constraint => IsHashAlgorithm(constraint, hashAlgorithmType));
        }

        for (var current = type as INamedTypeSymbol;
             current is not null;
             current = current.BaseType)
        {
            if (SymbolEqualityComparer.Default.Equals(current, hashAlgorithmType))
                return true;
        }

        return false;
    }

    private static bool IsPartial(INamedTypeSymbol type)
    {
        return type.DeclaringSyntaxReferences.Length > 0 &&
               type.DeclaringSyntaxReferences.All(reference =>
                   reference.GetSyntax() is TypeDeclarationSyntax declaration &&
                   declaration.Modifiers.Any(SyntaxKind.PartialKeyword));
    }

    private static IReadOnlyList<INamedTypeSymbol> GetContainingTypes(
        INamedTypeSymbol type)
    {
        var types = new List<INamedTypeSymbol>();
        for (var current = type.ContainingType;
             current is not null;
             current = current.ContainingType)
        {
            types.Add(current);
        }
        return types;
    }

    private static IReadOnlyList<INamedTypeSymbol> GetTypeChain(
        INamedTypeSymbol type)
    {
        var types = GetContainingTypes(type).ToList();
        types.Reverse();
        types.Add(type);
        return types;
    }

    private static int GetIndent(
        INamedTypeSymbol type,
        IReadOnlyList<INamedTypeSymbol> typeChain,
        INamedTypeSymbol container)
    {
        var namespaceIndent = container.ContainingNamespace.IsGlobalNamespace ? 0 : 1;
        for (var i = 0; i < typeChain.Count; i++)
        {
            if (SymbolEqualityComparer.Default.Equals(typeChain[i], type))
                return namespaceIndent + i;
        }
        return namespaceIndent;
    }

    private static void AppendIndent(StringBuilder sb, int indent) =>
        sb.Append(' ', indent * 4);

    private static int GetSourceOrder(IMethodSymbol method)
    {
        var location = method.Locations.FirstOrDefault(item => item.IsInSource);
        return location?.SourceSpan.Start ?? int.MaxValue;
    }

    private static Location GetDiagnosticLocation(
        AttributeData attribute,
        IMethodSymbol method,
        CancellationToken cancellationToken)
    {
        return attribute.ApplicationSyntaxReference?
                   .GetSyntax(cancellationToken)
                   .GetLocation() ??
               method.Locations.FirstOrDefault() ??
               Location.None;
    }

    private sealed record GenerationTarget(
        IMethodSymbol Factory,
        IMethodSymbol? Converter,
        ITypeSymbol ResultType);
}
