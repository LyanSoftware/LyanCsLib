using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using Lytec.Analyzer;
using System.Runtime.InteropServices;

namespace Lytec.Common.Data;

//[Generator]
//public class AutoSerializeGenerator : IIncrementalGenerator
//{
//    public void Initialize(IncrementalGeneratorInitializationContext context)
//    {
//        var types = context.SyntaxProvider
//            .CreateSyntaxProvider(
//                predicate: static (n, _) => n is TypeDeclarationSyntax t && t.AttributeLists.Count > 0,
//                transform: static (ctx, _) => GetTarget(ctx))
//            .Where(static t => t is not null);

//        var combined = context.CompilationProvider.Combine(types.Collect());

//        context.RegisterSourceOutput(combined, (spc, source) =>
//        {
//            Execute(source.Left, source.Right, spc);
//        });
//    }

//    static INamedTypeSymbol? GetTarget(GeneratorSyntaxContext ctx)
//    {
//        var decl = (TypeDeclarationSyntax)ctx.Node;
//        var symbol = ctx.SemanticModel.GetDeclaredSymbol(decl) as INamedTypeSymbol;

//        if (symbol == null)
//            return null;

//        if (!symbol.HasAttr("AutoSerializeObjectAttribute"))
//            return null;

//        return symbol;
//    }

//    record FieldLayout(
//        string Name,
//        ITypeSymbol Type,
//        int Offset,
//        int Size,
//        int? ArrayLen,
//        bool IsNested
//    );

//    static (List<FieldLayout> fields, int size) BuildLayout(INamedTypeSymbol type)
//    {
//        var fields = new List<FieldLayout>();
//        int offset = 0;

//        foreach (var m in type.GetMembers())
//        {
//            if (m is not IFieldSymbol f) continue;
//            if (!f.HasAttr("AutoSerializeAttribute")) continue;

//            var size = GetSize(f, out int? arrayLen, out bool nested);

//            fields.Add(new FieldLayout(
//                f.Name,
//                f.Type,
//                offset,
//                size,
//                arrayLen,
//                nested
//            ));

//            offset += size;
//        }

//        return (fields, offset);
//    }

//    static int GetSize(IFieldSymbol f, out int? arrayLen, out bool nested)
//    {
//        arrayLen = null;
//        nested = false;

//        var t = f.Type;

//        // array
//        if (t is IArrayTypeSymbol arr)
//        {
//            arrayLen = GetArrayLen(f);
//            return arr.ElementType.GetPrimitiveSize() * arrayLen.Value;
//        }

//        // nested
//        if (t.HasAttr("AutoSerializeObjectAttribute"))
//        {
//            nested = true;
//            return GetNestedSize(t);
//        }

//        return t.GetPrimitiveSize();
//    }

//    static int GetArrayLen(IFieldSymbol f)
//    {
//        var attr = f.GetAttributes()
//            .FirstOrDefault(a => a.AttributeClass?.Name == "MarshalAsAttribute") ?? throw new Exception("Array missing MarshalAs(SizeConst)");

//        foreach (var arg in attr.NamedArguments)
//            if (arg.Key == "SizeConst")
//                return (int)arg.Value.Value!;

//        throw new Exception("SizeConst missing");
//    }

//    static string GenerateSerialize(string name, List<FieldLayout> fields)
//    {
//        var sb = new System.Text.StringBuilder();

//        sb.AppendLine($"public void Serialize(Span<byte> buffer)");
//        sb.AppendLine("{");

//        foreach (var f in fields)
//        {
//            EmitWrite(sb, f);
//        }

//        sb.AppendLine("}");
//        return sb.ToString();
//    }

//    static void EmitWrite(System.Text.StringBuilder sb, FieldLayout f)
//    {
//        var name = f.Name;
//        var off = f.Offset;

//        if (f.IsNested)
//        {
//            sb.AppendLine($"this.{name}.Serialize(buffer.Slice({off}, {f.Size}));");
//            return;
//        }

//        if (f.ArrayLen.HasValue)
//        {
//            sb.AppendLine($"System.Buffer.BlockCopy(this.{name}, 0, buffer.ToArray(), {off}, {f.Size});");
//            return;
//        }

//        sb.AppendLine($"System.Buffers.Binary.BinaryPrimitives.WriteInt32LittleEndian(buffer.Slice({off}, {f.Size}), this.{name});");
//    }

//    static string GenerateDeserialize(string name, List<FieldLayout> fields)
//    {
//        var sb = new System.Text.StringBuilder();

//        sb.AppendLine($"public static {name} Deserialize(ReadOnlySpan<byte> buffer)");
//        sb.AppendLine("{");

//        sb.AppendLine($"var obj = new {name}();");

//        foreach (var f in fields)
//        {
//            EmitRead(sb, f);
//        }

//        sb.AppendLine("return obj;");
//        sb.AppendLine("}");
//        return sb.ToString();
//    }

//    static void EmitRead(System.Text.StringBuilder sb, FieldLayout f)
//    {
//        var name = f.Name;
//        var off = f.Offset;

//        if (f.IsNested)
//        {
//            sb.AppendLine($"obj.{name} = {f.Type.Name}.Deserialize(buffer.Slice({off}, {f.Size}));");
//            return;
//        }

//        if (f.ArrayLen.HasValue)
//        {
//            sb.AppendLine($"buffer.Slice({off}, {f.Size}).CopyTo(obj.{name});");
//            return;
//        }

//        sb.AppendLine($"obj.{name} = System.Buffers.Binary.BinaryPrimitives.ReadInt32LittleEndian(buffer.Slice({off}, {f.Size}));");
//    }

//    static void Execute(Compilation compilation, ImmutableArray<INamedTypeSymbol?> types, SourceProductionContext ctx)
//    {
//        foreach (var t in types)
//        {
//            if (t == null)
//                continue;

//            var (fields, size) = BuildLayout(t);

//            var code =
//    @$"
//public partial struct {t.Name}
//{{
//        public const int SizeConst = {size};

//    {GenerateSerialize(t.Name, fields)}

//    {GenerateDeserialize(t.Name, fields)}
//}}

//";

//            ctx.AddSource($"{t.Name}.g.cs", code);
//        }
//    }
//}
