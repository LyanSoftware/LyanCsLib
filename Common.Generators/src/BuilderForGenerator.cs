using System.Collections.Immutable;
using System.Diagnostics;
using System.Runtime.Serialization;
using System.Text;
using Lytec.Analyzer;
using Lytec.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lytec.Common.Generators;

[Generator]
public class BuilderForGenerator : IIncrementalGenerator
{
    private static readonly DiagnosticDescriptor BuilderContainerNotPartial = new(
        id: "LYTEC_COMMON_BUILDER_001",
        title: "Syntax error",
        messageFormat: "Type {0} using [BuilderFor] must be a partial class",
        category: "Syntax",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);
    
    private static readonly DiagnosticDescriptor BuilderContainerIsStatic = new(
        id: "LYTEC_COMMON_BUILDER_002",
        title: "Syntax error",
        messageFormat: "Type {0} using [BuilderFor] must not be a static class",
        category: "Syntax",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor TargetTypeMissingNoArgsConstructor = new(
        id: "LYTEC_COMMON_BUILDER_003",
        title: "Usage error",
        messageFormat: "Target type {0} must have an accessible parameterless constructor",
        category: "Usage",
        defaultSeverity: DiagnosticSeverity.Error,
        isEnabledByDefault: true);

    public const Accessibility BuilderAccessibility = Accessibility.Internal;
    private static bool IsAccessible(Accessibility accessibility)
    => accessibility.IsAccessible(BuilderAccessibility);

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        Debug.WriteLine("BuilderFor Generator Debug Init");

        var infos = context.SyntaxProvider.ForAttr<BuilderForAttribute>(node => node is ClassDeclarationSyntax or StructDeclarationSyntax);

        context.RegisterSourceOutput(context.CompilationProvider.Combine(infos), (spc, source) =>
        {
            var compilation = source.Left;
            var infos = source.Right;

            foreach (var (typeSymbol, attrs) in infos)
            {
                if (typeSymbol is not INamedTypeSymbol type)
                    continue;

                // 检查容器是否 partial（所有声明都必须 partial）
                if (!type.DeclaringSyntaxReferences.All(d =>
                {
                    var decl = d.GetSyntax();
                    return (decl is ClassDeclarationSyntax c && c.IsPartialType())
                        || (decl is StructDeclarationSyntax s && s.IsPartialType());
                }))
                {
                    foreach (var attr in attrs)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            BuilderContainerNotPartial,
                            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            type.Name));
                    }
                    continue;
                }
                
                // 检查容器是否 static（所有声明都不能 static）
                if (!type.DeclaringSyntaxReferences.All(d => d.GetSyntax() is ClassDeclarationSyntax c && c.IsPartialType()))
                {
                    foreach (var attr in attrs)
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            BuilderContainerIsStatic,
                            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            type.Name));
                    }
                    continue;
                }

                // 收集目标类型及其对应的特性实例
                var targets = new List<(INamedTypeSymbol Type, AttributeData Attr)>();
                foreach (var attr in attrs)
                {
                    if (attr.ConstructorArguments.Length == 0 ||
                        attr.ConstructorArguments[0].Value is not INamedTypeSymbol target)
                    {
                        continue;
                    }

                    // 验证目标类型是否有可访问的无参构造器
                    if (!type.HasNoArgsConstructor(BuilderAccessibility))
                    {
                        spc.ReportDiagnostic(Diagnostic.Create(
                            TargetTypeMissingNoArgsConstructor,
                            attr.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            target.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat)));
                        continue;
                    }

                    targets.Add((target, attr));
                }

                if (targets.Count == 0)
                    continue;

                // 生成源代码
                var fileName = $"{type.GetFiltedMetadataName()}.BuilderFor.g.cs";
                spc.AddSource(fileName, SourceText.From(GenerateSource(type, targets, compilation), Encoding.UTF8));
            }
        });
    }

    record Options(bool IncludeInternals = false, bool IncludeObsolete = false)
    {
        public static Options FromAttr(AttributeData attr)
        {
            var includeInternals = attr.NamedArguments.FirstOrDefault(kv => kv.Key == "IncludeInternals").Value.Value as bool? ?? false;
            var includeObsolete = attr.NamedArguments.FirstOrDefault(kv => kv.Key == "IncludeObsolete").Value.Value as bool? ?? false;
            return new Options(IncludeInternals: includeInternals, IncludeObsolete: includeObsolete);
        }
    }

    private static string GenerateSource(
        INamedTypeSymbol container,
        IReadOnlyList<(INamedTypeSymbol Type, AttributeData Attr)> targets,
        Compilation compilation
        )
    {
        var sb = new StringBuilder();
        var containers = GetContainers(container);

        sb.AppendLine("// <auto-generated />");
        sb.AppendLine("#nullable enable");

        foreach (var line in containers)
            sb.AppendLine(line);

        sb.Append(GetDeclarationHeader(container)).AppendLine();
        AppendConstraints(sb, container);
        sb.AppendLine("{");

        foreach (var (target, attr) in targets)
        {
            AppendBuilder(sb, container, target, Options.FromAttr(attr), compilation);
        }

        sb.AppendLine("}");

        for (var i = 0; i < containers.Count; i++)
            sb.AppendLine("}");

        sb.AppendLine("#nullable restore");

        return CSharpSyntaxTree.ParseText(sb.ToString())
            .GetRoot()
            .NormalizeWhitespace()
            .ToFullString();
    }

    private static void AppendBuilder(StringBuilder sb, INamedTypeSymbol container, INamedTypeSymbol target, Options options, Compilation compilation)
    {
        var builderName = GetDeclarationName(container);
        var targetTypeName = GetTypeName(target);
        var members = GetSettableMembers(target, options, compilation).ToArray();

        foreach (var member in members)
        {
            sb.Append("        private ")
                .Append(GetTypeName(member.Type))
                .Append(" ")
                .Append(GetFieldName(member.Name))
                .AppendLine(" = default!;");
        }

        if (members.Length > 0)
            sb.AppendLine();

        foreach (var member in members)
        {
            sb.Append("        public ").Append(builderName).Append(" With").Append(member.Name)
                .Append("(").Append(GetTypeName(member.Type)).Append(" value)").AppendLine();
            sb.AppendLine("        {");
            sb.Append("            ").Append(GetFieldName(member.Name)).AppendLine(" = value;");
            sb.AppendLine("            return this;");
            sb.AppendLine("        }");
            sb.AppendLine();
        }

        sb.Append("        public ").Append(targetTypeName).AppendLine(" Build()");
        sb.AppendLine("        {");
        sb.Append("            return new ").Append(targetTypeName).AppendLine();
        sb.AppendLine("            {");

        foreach (var member in members)
        {
            sb.Append("                ").Append(member.Name).Append(" = ").Append(GetFieldName(member.Name)).AppendLine(",");
        }

        sb.AppendLine("            };");
        sb.AppendLine("        }");
        sb.AppendLine();
    }

    private static IEnumerable<(string Name, ITypeSymbol Type)> GetSettableMembers(INamedTypeSymbol target, Options options, Compilation compilation)
    {
        foreach (var member in target.GetMembers())
        {
            // 检查是否标记了 [IgnoreDataMember]
            if (member.HasAttr<IgnoreDataMemberAttribute>(compilation))
                continue;

            // [Obsolete]
            if (!options.IncludeObsolete && member.HasAttr<ObsoleteAttribute>(compilation))
                continue;
            // internal
            if (!options.IncludeInternals && member.DeclaredAccessibility == Accessibility.Internal)
                continue;
            // non public
            if (!IsAccessible(member.DeclaredAccessibility))
                continue;

            switch (member)
            {
                case IPropertySymbol { IsStatic: false, IsIndexer: false, SetMethod: not null } property
                    when IsAccessible(property.SetMethod!.DeclaredAccessibility):
                    yield return (member.Name, property.Type);
                    break;

                case IFieldSymbol { IsStatic: false, IsReadOnly: false, IsConst: false } field:
                    yield return (member.Name, field.Type);
                    break;
            }
        }
    }

    private static List<string> GetContainers(INamedTypeSymbol type)
    {
        var containers = new List<string>();

        for (var containingType = type.ContainingType; containingType != null; containingType = containingType.ContainingType)
        {
            var sb = new StringBuilder();
            sb.AppendLine(GetDeclarationHeader(containingType));
            AppendConstraints(sb, containingType);
            sb.AppendLine("{");
            containers.Add(sb.ToString());
        }

        if (!type.ContainingNamespace.IsGlobalNamespace)
            containers.Add($"namespace {type.ContainingNamespace.ToDisplayString()}\r\n{{");

        containers.Reverse();
        return containers;
    }

    private static string GetFieldName(string memberName)
    {
        if (string.IsNullOrEmpty(memberName))
            return "_value";

        return "_" + char.ToLowerInvariant(memberName[0]) + memberName[1..];
    }

    private static string GetTypeName(ITypeSymbol type) => type.GetFullyQualifiedString();

    private static string GetDeclarationName(INamedTypeSymbol type)
    {
        if (type.TypeParameters.Length == 0)
            return type.Name;

        return type.Name + "<" + string.Join(", ", type.TypeParameters.Select(t => t.Name)) + ">";
    }

    private static string GetDeclarationHeader(INamedTypeSymbol type)
    => $"partial {type.GetTypeKeyword()} {GetDeclarationName(type)}";

    private static void AppendConstraints(StringBuilder sb, INamedTypeSymbol type)
    {
        foreach (var typeParameter in type.TypeParameters)
        {
            var constraints = typeParameter.GetConstraints().ToArray();
            if (constraints.Length == 0)
                continue;

            sb.Append("    where ")
                .Append(typeParameter.Name)
                .Append(" : ")
                .Append(string.Join(", ", constraints))
                .AppendLine();
        }
    }
}
