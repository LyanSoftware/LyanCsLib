using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

using Lytec.Analyzer;

namespace Lytec.Common.Data;

//[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class AutoSerializeGeneratorAnalyzer //  : DiagnosticAnalyzer
{
    internal static class DiagnosticDescriptors
    {
        public static readonly DiagnosticDescriptor AutoSerializeObjectMissingNoArgsConstructor = new(
            id: "LYTEC_COMMON_DATA_AUTO_SERIALIZE_001",
            title: "用法错误",
            messageFormat: "使用[AutoSerializeObject]的类型 {0} 必须具有无参数构造函数",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "使用[AutoSerializeObject]的类型必须具有无参数构造函数"
            );

        public static readonly DiagnosticDescriptor AutoSerializeObjectNotPartial = new(
            id: "LYTEC_COMMON_DATA_AUTO_SERIALIZE_002",
            title: "语法错误",
            messageFormat: "使用[AutoSerializeObject]的类型 {0} 必须是 partial 的",
            category: "Syntax",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: "使用[AutoSerializeObject]的类型必须是 partial 的"
            );
        
        public static readonly DiagnosticDescriptor NotSupportedMember = new(
            id: "LYTEC_COMMON_DATA_AUTO_SERIALIZE_003",
            title: "用法错误",
            messageFormat: @"成员 {0} 必须是满足以下条件之一，才能使用[AutoSerialize]：
是基元类型/枚举/数组
是[AutoSerializeObject]
实现了IAutoSerializeObject<T>，才能自动使用[AutoSerialize]",
            category: "Usage",
            defaultSeverity: DiagnosticSeverity.Error,
            isEnabledByDefault: true,
            description: @"成员必须是满足以下条件之一，才能使用[AutoSerialize]：
是基元类型/枚举/数组
是[AutoSerializeObject]
实现了IAutoSerializeObject<T>，才能自动使用[AutoSerialize]"
            );
        
    }

    //public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics { get; } = ImmutableArray.Create(
    //    typeof(DiagnosticDescriptors).GetFields()
    //        .Where(f => f.FieldType == typeof(DiagnosticDescriptor) && f.IsStatic)
    //        .Select(f => (DiagnosticDescriptor)f.GetValue(null))
    //        .ToArray()
    //    );

    //public override void Initialize(AnalysisContext context)
    //{
    //    context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
    //    context.EnableConcurrentExecution();
    //    context.RegisterSyntaxNodeAction(AnalyzeTypeDecl, SyntaxKind.ClassDeclaration);
    //    context.RegisterSyntaxNodeAction(AnalyzeTypeDecl, SyntaxKind.StructDeclaration);
    //    context.RegisterSyntaxNodeAction(AnalyzeTypeDecl, SyntaxKind.RecordDeclaration);
    //}

    //private void AnalyzeTypeDecl(SyntaxNodeAnalysisContext context)
    //{
    //    var model = context.SemanticModel;

    //    foreach (var typeDecl in context.Node.SyntaxTree.GetRoot().DescendantNodes()
    //                                    .OfType<TypeDeclarationSyntax>())
    //    {
    //        var typeSymbol = model.GetDeclaredSymbol(typeDecl);
    //        if (typeSymbol == null)
    //            continue;
    //        var attr = typeDecl.AttributeLists.SelectMany(al => al.Attributes)
    //            .FirstOrDefault(a => a != null
    //                && model.GetSymbolInfo(a).Symbol?.GetAttributes().Any(a => a.AttributeClass?.Name == nameof(AutoSerializeObjectAttribute)) == true);
    //        if (attr != null)
    //        {
    //            if (!typeSymbol.IsEnum() && !typeSymbol.HasNoArgsConstructor())
    //                context.ReportDiagnostic(Diagnostic.Create(
    //                    DiagnosticDescriptors.AutoSerializeObjectMissingNoArgsConstructor,
    //                    attr.GetLocation(),
    //                    new[] { typeSymbol.Name }
    //                    ));
    //            if (!typeSymbol.DeclaringSyntaxReferences[0].GetSyntax().IsPartialType())
    //                context.ReportDiagnostic(Diagnostic.Create(
    //                    DiagnosticDescriptors.AutoSerializeObjectNotPartial,
    //                    attr.GetLocation(),
    //                    new[] { typeSymbol.Name }
    //                    ));
    //        }
    //    }

    //}

}
