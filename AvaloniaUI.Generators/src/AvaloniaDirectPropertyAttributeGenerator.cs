using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using Avalonia;
using Avalonia.Data;
using DynamicData;
using Lytec.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lytec.AvaloniaUI.Generators;

[Generator]
public class AvaloniaDirectPropertyAttributeGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

//#if DEBUG
//        if (!Debugger.IsAttached)
//            Debugger.Launch();
//#endif
        Debug.WriteLine("generator debug init");
    }

    record Attr(string PropertyName = "", string UnsetValue = "", string BindingMode = "", string EnableDataValidation = "");

    private void Generate(GeneratorExecutionContext context)
    {
        // the generator infrastructure will create a receiver and populate it
        // we can retrieve the populated instance via the context
        if (context.SyntaxReceiver is SyntaxReceiver syntaxRcv)
        {
            Dictionary<string, (StringBuilder Builder, string End)> srcs = new();
            StringBuilder? getsb(MemberDeclarationSyntax syntax, out string TypeName)
            {
                TypeName = "";
                var model = context.Compilation.GetSemanticModel(syntax.SyntaxTree);
                var type = model.GetDeclaredSymbol(syntax)?.ContainingType;
                if (type == null)
                {
                    if (syntax is FieldDeclarationSyntax field)
                        type = model.GetDeclaredSymbol(field.Declaration.Variables[0])?.ContainingType;
                    if (type == null)
                        return null;
                }
                TypeName = type.Name;
                if (srcs.TryGetValue(TypeName, out var data))
                    return data.Builder;
                var usings = type.DeclaringSyntaxReferences
                    .Select(r => r.SyntaxTree.GetRoot())
                    .SelectMany(r => r.ChildNodes())
                    .Where(n => n is UsingDirectiveSyntax)
                    .SelectMany(n => n.ChildNodes())
                    .Select(n => n.ToFullString())
                    .ToDictionary(k => k);
                usings.Add("System", "");
                usings.Add("Avalonia", "");
                usings.Add("Avalonia.Data", "");
                var nsb = usings.Keys
                    .OrderBy(u => u)
                    .Aggregate(new StringBuilder(), (sb, line) => sb.Append("using ").Append(line).Append(";\r\n"));
                nsb.Append($@"
#nullable enable
");
                if (!type.ContainingNamespace.IsGlobalNamespace)
                    nsb.Append($@"
namespace {type.ContainingNamespace}
{{
");
                nsb.Append($@"
partial class {TypeName}
{{
");
                var end = "";
                if (!type.ContainingNamespace.IsGlobalNamespace)
                    end += "}";
                end += @"
}

#nullable restore
";
                srcs.Add(TypeName, (nsb, end));
                return nsb;
            }
            Attr parseAttr(IList<(string? Name, string Value)> args, string defaultPropertyName)
            {
                var offset = 0;
                string name = "";
                if (args.Count > 0 && args[0].Name.IsNullOrEmpty())
                {
                    name = args[0].Value;
                    offset++;
                }
                if (name == "")
                    name = defaultPropertyName;
                string val = "default";
                string mode = "BindingMode.OneWay";
                string dataval = "false";
                for (; offset < args.Count; offset++)
                {
                    switch (args[offset].Name)
                    {
                        case nameof(AvaloniaDirectPropertyAttribute.UnsetValue):
                            val = args[offset].Value;
                            break;
                        case nameof(AvaloniaDirectPropertyAttribute.BindingMode):
                            mode = args[offset].Value;
                            break;
                        case nameof(AvaloniaDirectPropertyAttribute.EnableDataValidation):
                            dataval = args[offset].Value;
                            break;
                    }
                }
                return new(name, val, mode, dataval);
            }
            foreach (var (prop, args) in syntaxRcv.PropertyList)
            {
                var sb = getsb(prop, out var className);
                if (sb == null)
                    continue;
                var type = prop.Type.ToString();
                var name = prop.Identifier.ValueText;
                var attr = parseAttr(args, $"\"{name}\"");
                if (context.Compilation
                    .GetSemanticModel(prop.SyntaxTree)
                    .GetDeclaredSymbol(prop)
                    is IPropertySymbol propsym
                    && propsym.GetMethod != null)
                {
                    sb.Append($@"
public static DirectProperty<{className}, {type}> {attr.PropertyName.Trim('"')}Property
= AvaloniaProperty.RegisterDirect<{className}, {type}>(
    {attr.PropertyName},
    o => o.{name},
");
                    sb.Append(propsym.SetMethod != null ? $"(o, v) => o.{name} = v," : "null,");
                    sb.Append($@"
    {attr.UnsetValue},
    {attr.BindingMode},
    {attr.EnableDataValidation}
);
");
                }
            }
            foreach (var (field, args) in syntaxRcv.FieldList)
            {
                var sb = getsb(field, out var className);
                if (sb == null)
                    continue;
                var type = field.Declaration.Type.ToString();
                var name = field.Declaration.Variables.First().Identifier.ValueText;
                var attr = parseAttr(args, $"\"{name}\"");
                sb.Append($@"
public static DirectProperty<{className}, {type}> {attr.PropertyName.Trim('"')}Property
= AvaloniaProperty.RegisterDirect<{className}, {type}>(
    {attr.PropertyName},
    o => o.{name},
    (o, v) => o.{name} = v,
    {attr.UnsetValue},
    {attr.BindingMode},
    {attr.EnableDataValidation}
);
");
            }

            foreach (var (className, (sb, end)) in srcs)
            {
                var src = CSharpSyntaxTree.ParseText(sb.Append(end).ToString())
                    .GetRoot()
                    .NormalizeWhitespace()
                    .ToFullString();
                context.AddSource($"{className}.Avalonia.DirectProperties.g.cs", SourceText.From(src, Encoding.UTF8));
            }
        }
    }

    public void Execute(GeneratorExecutionContext context)
    {
        try
        {
            Generate(context);
        }
        catch (Exception err)
        {
            Debug.WriteLine("generator err:\r\n" + err.ToString());
        }
    }

    class SyntaxReceiver : ISyntaxReceiver
    {
        static readonly string AttrSimpName = nameof(AvaloniaDirectPropertyAttribute)[..^9];

        public List<(FieldDeclarationSyntax Field, IList<(string? Name, string Value)> Attr)> FieldList { get; } = new();
        public List<(PropertyDeclarationSyntax Properties, IList<(string? Name, string Value)> Attr)> PropertyList { get; } = new();

        IList<(string? Name, string Value)> GetAttrArgs(AttributeSyntax attr)
        => attr.ArgumentList?
            .Arguments.Cast<AttributeArgumentSyntax>()
            .Select(arg => (arg.NameEquals?.Name.Identifier.ValueText, arg.Expression.NormalizeWhitespace().ToFullString()))
            .ToList() ?? (IList<(string?,string)>)Array.Empty<(string?, string)>();

        public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
        {
            // Business logic to decide what we're interested in goes here
            switch (syntaxNode)
            {
                case FieldDeclarationSyntax field:
                    foreach (var attrs in field.AttributeLists)
                    {
                        foreach (var attr in attrs.Attributes)
                        {
                            var name = attr.Name.ToString();
                            if (name == nameof(AvaloniaDirectPropertyAttribute) || name == AttrSimpName)
                            {
                                FieldList.Add((field, GetAttrArgs(attr)));
                                return;
                            }
                        }
                    }
                    break;
                case PropertyDeclarationSyntax prop:
                    foreach (var attrs in prop.AttributeLists)
                    {
                        foreach (var attr in attrs.Attributes)
                        {
                            var name = attr.Name.ToString();
                            if (name == nameof(AvaloniaDirectPropertyAttribute) || name == AttrSimpName)
                            {
                                PropertyList.Add((prop, GetAttrArgs(attr)));
                                return;
                            }
                        }
                    }
                    break;
            }
        }
    }
}
