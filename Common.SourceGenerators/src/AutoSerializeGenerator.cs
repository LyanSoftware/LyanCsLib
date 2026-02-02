using System.Diagnostics;
using System.Text;
using Lytec.Analyzer;
using Lytec.Common;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lytec.Common.Data;

using ObjAttr = AutoSerializeObjectAttribute;
using DataAttr = AutoSerializeAttribute;
using DiagnosticDescriptors = AutoSerializeGeneratorAnalyzer.DiagnosticDescriptors;

[Generator]
public class AutoSerializeGenerator : ISourceGenerator
{
    public void Initialize(GeneratorInitializationContext context)
    {
        // Register a factory that can create our custom syntax receiver
        //context.RegisterForSyntaxNotifications(() => new SyntaxReceiver());

//#if DEBUG
//        if (!Debugger.IsAttached)
//            Debugger.Launch();
//#endif
        Debug.WriteLine("AutoSerialize Generator Debug Init");
    }

    record AttrData<T>(T Attr, AttributeData Data) where T : Attribute;

    record MemberData(ISymbol Symbol, Endian? Endian, AttrData<DataAttr> AttrData);

    class ObjData
    {
        public AttrData<ObjAttr>? AttrData { get; set; }
        public Endian? Endian { get; set; } = null;
        public List<MemberData> Members { get; set; } = new();
    }

    static Endian? GetEndian(TypedConstant arg) => arg.Kind == TypedConstantKind.Enum ? arg.Value switch
    {
        (int)Endian.Big => Endian.Big,
        (int)Endian.Little => Endian.Little,
        _ => null,
    } : null;

    void Generate(GeneratorExecutionContext context)
    {

        var endianAttrSymbol = context.Compilation.GetTypeSymbol<EndianAttribute>();
        var objImplSymbol = context.Compilation.GetTypeSymbol(typeof(IAutoSerializeObject<>));
        var objAttrSymbol = context.Compilation.GetTypeSymbol<ObjAttr>();
        var dataAttrSymbol = context.Compilation.GetTypeSymbol<DataAttr>();

        if (endianAttrSymbol == null || objImplSymbol == null || objAttrSymbol == null || dataAttrSymbol == null)
            return;

        Dictionary<INamedTypeSymbol, ObjData> types = new();
        foreach (var syntaxTree in context.Compilation.SyntaxTrees)
        {
            var model = context.Compilation.GetSemanticModel(syntaxTree);

            foreach (var typeDecl in syntaxTree.GetRoot().DescendantNodes()
                                            .OfType<TypeDeclarationSyntax>())
            {
                var typeSymbol = model.GetDeclaredSymbol(typeDecl);
                if (typeSymbol == null)
                    continue;

                AttrData<ObjAttr>? objAttr = null;
                Endian? typeEndian = null;
                foreach (var attrData in typeSymbol.GetAttributes())
                {
                    if (objAttr == null && attrData.AttributeClass.IsEqualsTo(objAttrSymbol))
                        objAttr = new(new(), attrData);
                    else if (typeEndian == null && attrData.AttributeClass.IsEqualsTo(endianAttrSymbol))
                        typeEndian ??= GetEndian(attrData.ConstructorArguments[0]);
                    if (objAttr != null && typeEndian != null)
                        break;
                }

                if (objAttr != null)
                {
                    if (!typeSymbol.IsEnum() && !typeSymbol.HasNoArgsConstructor())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.AutoSerializeObjectMissingNoArgsConstructor,
                            objAttr.Data.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            new[] { typeSymbol.Name }
                            ));
                        continue;
                    }
                    if (!typeSymbol.DeclaringSyntaxReferences[0].GetSyntax().IsPartialType())
                    {
                        context.ReportDiagnostic(Diagnostic.Create(
                            DiagnosticDescriptors.AutoSerializeObjectNotPartial,
                            objAttr.Data.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                            new[] { typeSymbol.Name }
                            ));
                        continue;
                    }

                    foreach (var (name, arg) in objAttr.Data.NamedArguments)
                    {
                        switch (name)
                        {
                            case nameof(ObjAttr.SerializeMethodName):
                                if (arg.Value is string serializerName)
                                {
                                    objAttr.Attr.SerializeMethodName = serializerName;
                                }
                                break;
                        }
                    }

                    foreach (var memberSymbol in typeSymbol.GetMembers()
                                .Where(m => m.Kind == SymbolKind.Property || m.Kind == SymbolKind.Field))
                    {
                        AttrData<DataAttr>? dataAttr = null;
                        Endian? dataEndian = null;
                        foreach (var a in memberSymbol.GetAttributes())
                        {
                            if (dataAttr == null && a.AttributeClass.IsEqualsTo(dataAttrSymbol))
                                dataAttr = new(new(), a);
                            else if (dataEndian == null && a.AttributeClass.IsEqualsTo(endianAttrSymbol))
                                dataEndian = GetEndian(a.ConstructorArguments[0]);

                            if (dataAttr != null && dataEndian != null)
                                break;
                        }

                        if (dataAttr != null)
                        {
                            if (memberSymbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.IsSubtypeOf(dataAttrSymbol) == true) != null)
                            {
                                if (!types.TryGetValue(typeSymbol, out var data))
                                    types[typeSymbol] = data = new();
                                data.Members.Add(new(memberSymbol, dataEndian ?? typeEndian, dataAttr));
                                if (objAttr != null && data.AttrData == null)
                                    data.AttrData = objAttr;
                                if (typeEndian != null && data.Endian == null)
                                    data.Endian = typeEndian;
                            }
                        }
                    }
                }
            }
        }

        foreach (var (type, typeData) in types)
        {
            if (typeData.AttrData == null)
                continue;

            try
            {
                var objImplSymbols = new Dictionary<string, INamedTypeSymbol>();

                var sb = new StringBuilder();
                var containers = new List<(string Name, string Line)>();
                {
                    var usings = type.DeclaringSyntaxReferences
                        .Select(r => r.SyntaxTree.GetRoot())
                        .SelectMany(r => r.ChildNodes())
                        .Where(n => n is UsingDirectiveSyntax)
                        .SelectMany(n => n.ChildNodes())
                        .Select(n => n.ToFullString())
                        .Append("System")
                        .ToHashSet();
                    foreach (var ns in usings.OrderBy(n => n))
                        sb.Append("using ").Append(ns).Append(";\r\n");
                    sb.Append($@"
#nullable enable
");
                    for (var t = type.ContainingType; t != null; t = t.ContainingType)
                        containers.Add((t.Name, $"partial {t.GetTypeKeyword()} {t.Name} {{"));
                    if (!type.ContainingNamespace.IsGlobalNamespace)
                        containers.Add((type.ContainingNamespace.ToDisplayString(), $"namespace {type.ContainingNamespace.ToDisplayString()} {{"));
                    containers.Reverse();
                    foreach (var (_, Line) in containers)
                        sb.Append(Line);
                }
                var serialize = new StringBuilder();
                serialize.Append("var buf = new List<byte>();\r\n");
                var deserialize = new StringBuilder();
                var members = new List<(string Name, string Type, bool IsPrimitive, int Size, Endian? Endian)>();
                foreach (var (symbol, endian, dataAttr) in typeData.Members)
                {
                    if (dataAttr == null)
                        continue;

                    ITypeSymbol typeSymbol;
                    switch (symbol)
                    {
                        case IPropertySymbol prop:
                            typeSymbol = prop.Type;
                            break;
                        case IFieldSymbol field:
                            typeSymbol = field.Type;
                            break;
                        default:
                            continue;
                    }
                    /*
                     m.IsPrimitive ?
                    $"Lytec.Common.Data.StructHelper.PrimitiveToBytes({m.Name}, {m.Endian})"
                    :
                    $"((Lytec.Common.Data.IAutoSerializeObject)m).Serialize()"

                     */
                    var endianStr = endian switch
                    {
                        Endian.Big => "Lytec.Common.Data.Endian.Big",
                        Endian.Little => "Lytec.Common.Data.Endian.Little",
                        _ => "Lytec.Common.Data.EndianUtils.LocalEndian",
                    };
                    switch (typeSymbol)
                    {
                        case INamedTypeSymbol named:
                            if (named.IsPrimitiveOrEnum())
                            {
                                members.Add((symbol.Name, typeSymbol.Name, true, typeSymbol.GetPrimitiveSize(), typeData.Endian));
                                serialize.Append($"buf.AddRange(Lytec.Common.Data.StructHelper.PrimitiveToBytes({named.GetFullName()}, {endianStr}));\r\n");
                            }
                            if (named.IsSubtypeOf(objImplSymbol))
                            {

                            }
                            break;
                        case IArrayTypeSymbol array:
                            {

                            }
                            break;
                        case ITypeParameterSymbol generic:
                            break;
                        default:
                            context.ReportDiagnostic(Diagnostic.Create(
                                DiagnosticDescriptors.NotSupportedMember,
                                dataAttr.Data.ApplicationSyntaxReference?.GetSyntax().GetLocation(),
                                new[] { typeSymbol.Name }
                                ));
                            throw new ApplicationException();
                    }
                }
                serialize.Append("return buf.ToArray();\r\n");

                sb.Append($@"
partial {type.GetTypeKeyword()} {type.Name} : Lytec.Common.Data.IAutoSerializeObject<{type.Name}>
{{
    class Lytec_AutoSerialization_Deserializer : Lytec.Common.Serialization.IVariableLengthDeserializer<{type.Name}>
    {{
        public {type.Name}? Deserialize(IEnumerable<byte> data, out int DeserializedLength)
        {{
            DeserializedLength = -1;
            var offset = 0;
            var t = new {type.Name}();
            {members.JoinToString(m => $@"
                {{
                {(
                 m.IsPrimitive ?
                 $@"
                    var buf = data.Skip(offset).Take({m.Size}).ToArray();
                    if (buf.Length < {m.Size})
                        return null;
                    {(
                     m.Size > 1 && m.Endian != null ? $@"
                        if (Lytec.Common.Data.EndianUtils.LocalEndian != Lytec.Common.Data.Endian.{(m.Endian == Endian.Big ? "Big" : "Little")})
                            Array.Reverse(buf);
" : ""
                 )}
                    t.{m.Name} = System.Runtime.InteropServices.MemoryMarshal.Read<{m.Type}>(buf);
                    offset += {m.Size};
"
                 :
                 $@"
                    if (Lytec.Common.Data.AutoSerializeUtils.GetDeserializer<{m.Type}>().Deserialize(data.Skip(offset), out var len) is {m.Type} v)
                    {{
                        t.{m.Name} = v;
                        offset += len;
                    }}
                    else return null;
"
             )}
                }}", "")}
            DeserializedLength = offset;
            return t;
        }}

        public {type.Name}? Deserialize(ReadOnlySpan<byte> data, out int DeserializedLength)
        {{
            throw new NotImplementedException();
        }}

        public {type.Name}? Deserialize(IEnumerable<byte> data)
        => Deserialize(data, out _);

        public {type.Name}? Deserialize(ReadOnlySpan<byte> data)
        => Deserialize(data, out _);
    }}

    Lytec.Common.Serialization.IVariableLengthDeserializer<{type.Name}> Lytec.Common.IFactory<Lytec.Common.Serialization.IVariableLengthDeserializer<{type.Name}>>.Create()
    => new Lytec_AutoSerialization_Deserializer();

#if NET6_0_OR_GREATER
    static Lytec.Common.Serialization.IVariableLengthDeserializer<{type.Name}> Lytec.Common.IFactory<Lytec.Common.Serialization.IVariableLengthDeserializer<{type.Name}>>.CreateInstance()
    => new Lytec_AutoSerialization_Deserializer();
#endif

}}
");
                {
                    var typeAttr = typeData.AttrData.Attr;
                    var serializerName = typeAttr.SerializeMethodName ?? ObjAttr.DefaultSerializeMethodName;
                    if (!typeAttr.SerializeMethodName.IsNullOrEmpty())
                        sb.Append($@"
    public virtual byte[] {serializerName}()
    {{
        {serialize}
    }}

    byte[] Lytec.Common.Data.IAutoSerializeObject.Serialize()
    => {serializerName}();
");
                    else sb.Append($@"
    byte[] Lytec.Common.Data.IAutoSerializeObject.Serialize()
    {{
        var buf = new List<byte>();
        {members.JoinToString(m => "buf.AddRange(" + (m.IsPrimitive ?
                    $"Lytec.Common.Data.StructHelper.PrimitiveToBytes({m.Name}, {m.Endian})"
                    :
                    $"((Lytec.Common.Data.IAutoSerializeObject)m).Serialize()"
                    ) + ");\r\n", ""
                )}
        return buf.ToArray();
    }}
");
                }

                for (var i = 0; i < containers.Count; i++)
                    sb.Append("\r\n}\r\n");
                sb.Append(@"
#nullable restore
");
                var src = CSharpSyntaxTree.ParseText(sb.ToString())
                    .GetRoot()
                    .NormalizeWhitespace()
                    .ToFullString();
                context.AddSource($"{containers.JoinToString(x => x.Name, ".")}.{type.Name}.AutoSerialize.g.cs", SourceText.From(src, Encoding.UTF8));
            }
            catch (ApplicationException)
            { }
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

}
