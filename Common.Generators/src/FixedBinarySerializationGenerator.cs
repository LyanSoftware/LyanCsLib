using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Lytec.Common.Generators;

/// <summary>为固定布局二进制对象生成无反射编解码代码。</summary>
[Generator]
public sealed class FixedBinarySerializationGenerator : IIncrementalGenerator
{
    private const string MarkerName = "Lytec.Common.Serialization.IBinarySerializable";
    private const string FixedBinarySerializableName = "Lytec.Common.Serialization.FixedBinarySerializableAttribute";
    private const string FixedBinaryContractName = "Lytec.Common.Serialization.FixedBinaryContractAttribute";
    private const string CodecProviderName = "Lytec.Common.Serialization.IFixedBinaryCodecProvider<T>";
    private const string BinaryMemberName = "Lytec.Common.Serialization.BinaryMemberAttribute";
    private const string BinaryIgnoreName = "Lytec.Common.Serialization.BinaryIgnoreAttribute";
    private const string BinaryFieldOffsetName = "Lytec.Common.Serialization.BinaryFieldOffsetAttribute";
    private const string BinaryFixedArrayName = "Lytec.Common.Serialization.BinaryFixedArrayAttribute";
    private const string BinaryDataTypeName = "Lytec.Common.Serialization.BinaryDataTypeAttribute";
    private const string EndianName = "Lytec.Common.Data.EndianAttribute";

    private static readonly DiagnosticDescriptor InvalidType = Error("LYBIN001", "类型不能生成固定二进制编解码代码", "类型“{0}”不受支持：{1}");
    private static readonly DiagnosticDescriptor InvalidMember = Error("LYBIN002", "成员不能参与固定二进制布局", "成员“{0}”不受支持：{1}");
    private static readonly DiagnosticDescriptor InvalidLayout = Error("LYBIN003", "固定二进制布局无效", "类型“{0}”的布局无效：{1}");
    private static readonly DiagnosticDescriptor ConflictingMember = Error("LYBIN004", "二进制成员标记冲突", "成员“{0}”同时具有互相冲突的包含和排除标记");
    private static readonly DiagnosticDescriptor ReservedMember = Error("LYBIN005", "成员名称由生成器保留", "类型“{0}”已经声明生成器保留的成员“{1}”");
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.CreateSyntaxProvider(
                static (node, _) => node is TypeDeclarationSyntax { AttributeLists.Count: > 0 },
                static (syntaxContext, _) => GetCandidate(syntaxContext))
            .Where(static symbol => symbol is not null)
            .Collect();

        context.RegisterSourceOutput(context.CompilationProvider.Combine(candidates), static (output, pair) =>
        {
            var distinct = new HashSet<INamedTypeSymbol>(SymbolEqualityComparer.Default);
            foreach (var type in pair.Right.OfType<INamedTypeSymbol>())
                if (distinct.Add(type))
                    Generate(type, pair.Left, output);
        });
    }

    private static INamedTypeSymbol? GetCandidate(GeneratorSyntaxContext context)
    {
        if (context.SemanticModel.GetDeclaredSymbol(context.Node) is not INamedTypeSymbol type)
            return null;
        return HasDirectAttribute(type, FixedBinarySerializableName) ? type : null;
    }

    private static void Generate(INamedTypeSymbol type, Compilation compilation, SourceProductionContext output)
    {
        var planner = new Planner(compilation, output);
        var plan = planner.Build(type, true);
        if (plan is null || planner.HasErrors)
            return;

        output.AddSource(
            Sanitize(type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)) + ".FixedBinary.g.cs",
            SourceText.From(Emitter.Emit(plan), Encoding.UTF8));
    }

    private static DiagnosticDescriptor Error(string id, string title, string message)
        => new(id, title, message, "Usage", DiagnosticSeverity.Error, true);

    private static bool Implements(INamedTypeSymbol type, string metadataName)
        => type.AllInterfaces.Any(i => i.ToDisplayString() == metadataName);

    private static bool HasDirectAttribute(ISymbol symbol, string metadataName)
        => symbol.GetAttributes().Any(attribute => attribute.AttributeClass?.ToDisplayString() == metadataName);

    private static string Sanitize(string value)
    {
        var builder = new StringBuilder(value.Length);
        foreach (var character in value)
            builder.Append(char.IsLetterOrDigit(character) ? character : '_');
        return builder.ToString();
    }

    private enum LayoutKindValue { Sequential, Explicit }
    private enum DataKind { I1, U1, I2, U2, I4, U4, I8, U8, R4, R8, Bool4, Nested, Array }

    private sealed record TypePlan(
        INamedTypeSymbol Type,
        LayoutKindValue Layout,
        int Size,
        int Alignment,
        bool IsAbstract,
        TypePlan? Base,
        ImmutableArray<MemberPlan> Members);

    private sealed record MemberPlan(
        ISymbol Symbol,
        ITypeSymbol Type,
        string Name,
        int Offset,
        int Size,
        int Alignment,
        DataKind Kind,
        DataKind ElementKind,
        ITypeSymbol? ElementType,
        int ElementCount,
        int ElementSize,
        INamedTypeSymbol? CodecProvider,
        int? FixedEndian);

    private sealed class Planner
    {
        private readonly Compilation _compilation;
        private readonly SourceProductionContext _output;
        private readonly Dictionary<INamedTypeSymbol, TypePlan?> _cache = new(SymbolEqualityComparer.Default);
        private readonly HashSet<INamedTypeSymbol> _building = new(SymbolEqualityComparer.Default);

        public bool HasErrors { get; private set; }

        public Planner(Compilation compilation, SourceProductionContext output)
            => (_compilation, _output) = (compilation, output);

        public TypePlan? Build(INamedTypeSymbol type, bool root)
        {
            if (_cache.TryGetValue(type, out var cached))
                return cached;
            if (!_building.Add(type))
                return FailType(type, "检测到递归内联布局");

            var result = BuildCore(type, root);
            _building.Remove(type);
            _cache[type] = result;
            return result;
        }

        private TypePlan? BuildCore(INamedTypeSymbol type, bool root)
        {
            if (HasDirectAttribute(type, FixedBinaryContractName))
                return FailType(type, "FixedBinarySerializable 与 FixedBinaryContract 不能同时使用");
            if (type.TypeKind is not (TypeKind.Class or TypeKind.Struct) || type.IsRecord)
                return FailType(type, "只支持普通 partial class 或 partial struct");
            if (type.IsRefLikeType || type.IsStatic || type.Arity != 0 || HasGenericContainer(type))
                return FailType(type, "不支持静态、ref-like 或泛型类型");
            if (type.DeclaringSyntaxReferences.Length == 0
                || type.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is not TypeDeclarationSyntax d || !d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                return FailType(type, "类型及其所有声明必须使用 partial");
            for (var container = type.ContainingType; container is not null; container = container.ContainingType)
                if (container.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is not TypeDeclarationSyntax d || !d.Modifiers.Any(SyntaxKind.PartialKeyword)))
                    return FailType(type, $"包含类型“{container.Name}”必须使用 partial");

            var layout = GetLayout(type, out var explicitSize, out var inclusion);
            if (layout is null)
                return FailLayout(type, "FixedBinarySerializable 必须指定 Sequential 或 Explicit 布局");
            if (explicitSize < 0)
                return FailLayout(type, "Size 不能为负数");
            const int pack = 1;

            TypePlan? basePlan = null;
            if (type.TypeKind == TypeKind.Class && type.BaseType is { SpecialType: not SpecialType.System_Object } baseType)
            {
                if (HasStoredState(baseType))
                {
                    if (!HasDirectAttribute(baseType, FixedBinarySerializableName))
                        return FailType(type, $"具有实例存储的基类“{baseType.Name}”必须使用 FixedBinarySerializable 生成");
                    if (layout == LayoutKindValue.Explicit)
                        return FailLayout(type, "Explicit 布局不能通过继承扩展");
                    basePlan = Build(baseType, false);
                    if (basePlan is null || basePlan.Layout == LayoutKindValue.Explicit)
                        return FailLayout(type, "不能继承 Explicit 布局类型");
                }
                else if (!baseType.InstanceConstructors.Any(c => c.Parameters.Length == 0
                    && c.DeclaredAccessibility is Accessibility.Public or Accessibility.Protected or Accessibility.ProtectedOrInternal))
                {
                    return FailType(type, $"不参与布局的基类“{baseType.Name}”必须具有可访问的无参数构造函数");
                }
            }

            if (!root && type.IsAbstract)
            {
                // 抽象基类允许作为布局片段；内联成员会在成员分析处拒绝。
            }

            var storageMembers = GetStorageMembers(type).ToArray();
            if (layout == LayoutKindValue.Sequential)
            {
                var declarations = storageMembers.Select(GetDeclaration).Where(d => d is not null)
                    .Select(d => (d!.SyntaxTree, d.SpanStart))
                    .Distinct().Count();
                if (declarations > 1)
                    return FailLayout(type, "Sequential 布局的存储成员必须来自同一个 partial 声明");
            }

            var members = new List<MemberPlan>();
            var offset = basePlan?.Size ?? 0;
            var alignment = Math.Min(pack, basePlan?.Alignment ?? 1);
            foreach (var symbol in storageMembers.OrderBy(SourceOrder))
            {
                var include = HasRepresentation(symbol);
                var ignore = Has(symbol, BinaryIgnoreName);
                if (include && ignore)
                {
                    Report(ConflictingMember, symbol, symbol.Name);
                    continue;
                }
                if (ignore || (inclusion == 1 && !include))
                    continue;

                var member = CreateMember(symbol, type, layout, pack, ref offset);
                if (member is null)
                    continue;
                members.Add(member);
                alignment = Math.Max(alignment, Math.Min(pack, member.Alignment));
            }

            if (HasErrors)
                return null;
            if (layout == LayoutKindValue.Explicit && !ValidateOverlaps(type, members))
                return null;

            var naturalSize = layout == LayoutKindValue.Explicit
                ? members.Select(m => m.Offset + m.Size).DefaultIfEmpty(0).Max()
                : offset;
            if (explicitSize > 0 && explicitSize < naturalSize)
                return FailLayout(type, $"Size {explicitSize} 小于成员实际占用大小 {naturalSize}");
            var size = Math.Max(1, explicitSize == 0 ? naturalSize : explicitSize);

            if (Reserved(type, type.IsAbstract))
                return null;

            return new TypePlan(type, layout.Value, size, 1, type.IsAbstract, basePlan, members.ToImmutableArray());
        }

        private MemberPlan? CreateMember(ISymbol symbol, INamedTypeSymbol owner, LayoutKindValue? layout, int pack, ref int sequentialOffset)
        {
            var memberType = symbol switch { IFieldSymbol f => f.Type, IPropertySymbol p => p.Type, _ => null };
            if (memberType is null || memberType.NullableAnnotation == NullableAnnotation.Annotated)
                return FailMember(symbol, "不支持可空成员");

            var fixedArray = GetAttribute(symbol, BinaryFixedArrayName);
            var dataTypeAttribute = GetAttribute(symbol, BinaryDataTypeName);
            var fixedEndian = GetEndian(symbol) ?? GetEndian(memberType) ?? GetInheritedEndian(owner);
            if (fixedEndian is not null and not 0 and not 1)
                return FailMember(symbol, "EndianAttribute 的值无效");
            DataKind kind;
            DataKind elementKind = default;
            ITypeSymbol? elementType = null;
            INamedTypeSymbol? codecProvider = null;
            var elementCount = 0;
            var elementSize = 0;
            int size;
            int alignment;

            if (memberType is IArrayTypeSymbol array)
            {
                if (array.Rank != 1)
                    return FailMember(symbol, "只支持一维数组");
                if (fixedArray is null)
                    return FailMember(symbol, "数组必须使用 BinaryFixedArray");
                if (dataTypeAttribute is not null)
                    return FailMember(symbol, "数组元素表示必须通过 BinaryFixedArray.ElementType 指定");
                elementCount = GetCtorInt(fixedArray);
                elementType = array.ElementType;
                if (elementCount <= 0)
                    return FailMember(symbol, "BinaryFixedArray 必须指定正数 Length");
                var elementDataType = GetNamedInt(fixedArray, "ElementType") ?? 0;
                fixedEndian = GetEndian(symbol) ?? GetEndian(elementType) ?? GetInheritedEndian(owner);
                if (elementType is INamedTypeSymbol nested && IsFixedContract(nested))
                {
                    if (elementDataType != 0)
                        return FailMember(symbol, "内联对象数组不能指定 BinaryDataType");
                    if (!TryGetInlineContract(nested, owner, out elementSize, out alignment, out codecProvider, out var error))
                        return error is null ? null : FailMember(symbol, error);
                    elementKind = DataKind.Nested;
                }
                else if (!TryDataType(elementType, elementDataType == 0 ? null : elementDataType, out elementKind, out elementSize, out alignment))
                    return FailMember(symbol, "数组元素的 BinaryDataType 与 CLR 类型不匹配、缺少必要表示或不受支持");
                kind = DataKind.Array;
                size = checked(elementCount * elementSize);
            }
            else if (memberType is INamedTypeSymbol named && IsFixedContract(named))
            {
                if (fixedArray is not null)
                    return FailMember(symbol, "BinaryFixedArray 只能用于数组成员");
                if (dataTypeAttribute is not null)
                    return FailMember(symbol, "内联对象不能指定 BinaryDataType");
                if (!TryGetInlineContract(named, owner, out size, out alignment, out codecProvider, out var error))
                    return error is null ? null : FailMember(symbol, error);
                kind = DataKind.Nested;
            }
            else
            {
                if (fixedArray is not null)
                    return FailMember(symbol, "BinaryFixedArray 只能用于数组成员");
                int? dataType = dataTypeAttribute is null ? null : GetCtorInt(dataTypeAttribute);
                if (dataType == 0)
                    return FailMember(symbol, "标量成员不能显式使用 BinaryDataType.Automatic");
                if (!TryDataType(memberType, dataType, out kind, out size, out alignment))
                    return FailMember(symbol, "只支持固定宽度基元、枚举或具有固定二进制契约的内联对象");
            }

            int offset;
            var fieldOffset = GetAttribute(symbol, BinaryFieldOffsetName);
            if (layout == LayoutKindValue.Explicit)
            {
                if (fieldOffset is null)
                    return FailMember(symbol, "Explicit 布局成员必须具有 BinaryFieldOffset");
                offset = GetCtorInt(fieldOffset);
                if (offset < 0)
                    return FailMember(symbol, "BinaryFieldOffset 不能为负数");
            }
            else
            {
                if (fieldOffset is not null)
                    return FailMember(symbol, "Sequential 布局成员不能使用 BinaryFieldOffset");
                var effectiveAlignment = Math.Min(pack, alignment);
                sequentialOffset = Align(sequentialOffset, effectiveAlignment);
                offset = sequentialOffset;
                sequentialOffset = checked(offset + size);
            }

            return new MemberPlan(symbol, memberType, symbol.Name, offset, size, alignment, kind,
                elementKind, elementType, elementCount, elementSize, codecProvider, fixedEndian);
        }

        private bool TryGetInlineContract(
            INamedTypeSymbol type,
            INamedTypeSymbol owner,
            out int size,
            out int alignment,
            out INamedTypeSymbol? codecProvider,
            out string? error)
        {
            size = 0;
            alignment = 1;
            codecProvider = null;
            error = null;
            if (type.IsAbstract)
            {
                error = "抽象类型不能作为内联成员";
                return false;
            }

            if (HasDirectAttribute(type, FixedBinarySerializableName))
            {
                var nestedPlan = Build(type, false);
                if (nestedPlan is null)
                    return false;
                size = nestedPlan.Size;
                return true;
            }

            var contract = type.GetAttributes().FirstOrDefault(
                attribute => attribute.AttributeClass?.ToDisplayString() == FixedBinaryContractName);
            if (contract is null || contract.ConstructorArguments.Length != 2)
            {
                error = "内联类型缺少固定二进制契约";
                return false;
            }
            if (!Implements(type, MarkerName))
            {
                error = "使用 FixedBinaryContract 的类型必须手动实现 IBinarySerializable";
                return false;
            }

            size = Convert.ToInt32(contract.ConstructorArguments[0].Value);
            codecProvider = contract.ConstructorArguments[1].Value as INamedTypeSymbol;
            if (size <= 0)
            {
                error = "FixedBinaryContract 的 Size 必须为正数";
                return false;
            }
            if (codecProvider is null || codecProvider.IsAbstract
                || codecProvider.TypeKind is not (TypeKind.Class or TypeKind.Struct))
            {
                error = "FixedBinaryContract 必须指定可构造的 codec provider 类型";
                return false;
            }
            if (!_compilation.IsSymbolAccessibleWithin(codecProvider, owner)
                || codecProvider.TypeKind == TypeKind.Class
                && !codecProvider.InstanceConstructors.Any(constructor => constructor.Parameters.Length == 0
                    && _compilation.IsSymbolAccessibleWithin(constructor, owner)))
            {
                error = "codec provider 类型及其无参数构造函数必须可访问";
                return false;
            }
            if (!codecProvider.AllInterfaces.Any(@interface =>
                    @interface.OriginalDefinition.ToDisplayString() == CodecProviderName
                    && SymbolEqualityComparer.Default.Equals(@interface.TypeArguments[0], type)))
            {
                error = $"codec provider 必须实现 IFixedBinaryCodecProvider<{type.Name}>";
                return false;
            }
            return true;
        }

        private static bool IsFixedContract(INamedTypeSymbol type)
            => HasDirectAttribute(type, FixedBinarySerializableName)
                || HasDirectAttribute(type, FixedBinaryContractName);

        private static bool TryDataType(ITypeSymbol type, int? dataType, out DataKind kind, out int size, out int alignment)
        {
            kind = default;
            size = alignment = 0;
            var special = type.TypeKind == TypeKind.Enum ? ((INamedTypeSymbol)type).EnumUnderlyingType!.SpecialType : type.SpecialType;
            if (special is SpecialType.System_Char or SpecialType.System_String or SpecialType.System_IntPtr or SpecialType.System_UIntPtr)
                return false;

            if (special == SpecialType.System_Boolean)
            {
                (kind, size) = dataType switch
                {
                    11 => (DataKind.U1, 1),
                    12 => (DataKind.U2, 2),
                    13 => (DataKind.Bool4, 4),
                    _ => (default, 0)
                };
                alignment = size;
                return size != 0;
            }

            if (dataType is not null)
            {
                (kind, size) = dataType switch
                {
                    1 => (DataKind.I1, 1), 2 => (DataKind.U1, 1),
                    3 => (DataKind.I2, 2), 4 => (DataKind.U2, 2),
                    5 => (DataKind.I4, 4), 6 => (DataKind.U4, 4),
                    7 => (DataKind.I8, 8), 8 => (DataKind.U8, 8),
                    9 => (DataKind.R4, 4), 10 => (DataKind.R8, 8),
                    _ => (default, 0)
                };
                alignment = size;
                return size != 0 && Compatible(special, kind);
            }

            (kind, size) = special switch
            {
                SpecialType.System_SByte => (DataKind.I1, 1), SpecialType.System_Byte => (DataKind.U1, 1),
                SpecialType.System_Int16 => (DataKind.I2, 2), SpecialType.System_UInt16 => (DataKind.U2, 2),
                SpecialType.System_Int32 => (DataKind.I4, 4), SpecialType.System_UInt32 => (DataKind.U4, 4),
                SpecialType.System_Int64 => (DataKind.I8, 8), SpecialType.System_UInt64 => (DataKind.U8, 8),
                SpecialType.System_Single => (DataKind.R4, 4), SpecialType.System_Double => (DataKind.R8, 8),
                _ => (default, 0)
            };
            alignment = size;
            return size != 0;
        }

        private static bool Compatible(SpecialType type, DataKind kind)
        {
            if (kind == DataKind.R4) return type == SpecialType.System_Single;
            if (kind == DataKind.R8) return type == SpecialType.System_Double;
            return type is SpecialType.System_SByte or SpecialType.System_Byte
                or SpecialType.System_Int16 or SpecialType.System_UInt16
                or SpecialType.System_Int32 or SpecialType.System_UInt32
                or SpecialType.System_Int64 or SpecialType.System_UInt64;
        }

        private bool ValidateOverlaps(INamedTypeSymbol type, List<MemberPlan> members)
        {
            for (var i = 0; i < members.Count; i++)
            for (var j = i + 1; j < members.Count; j++)
            {
                var left = members[i]; var right = members[j];
                if (left.Offset >= right.Offset + right.Size || right.Offset >= left.Offset + left.Size)
                    continue;
                var harmless = left.Size == 1 && right.Size == 1
                    || left.Offset == right.Offset && left.Size == right.Size
                    && left.Kind == right.Kind && SymbolEqualityComparer.Default.Equals(left.Type, right.Type)
                    && left.FixedEndian == right.FixedEndian;
                if (!harmless)
                {
                    FailLayout(type, $"成员“{left.Name}”与“{right.Name}”存在有歧义的多字节重叠");
                    return false;
                }
            }
            return true;
        }

        private bool Reserved(INamedTypeSymbol type, bool abstractType)
        {
            var names = new HashSet<string> { "SerializedSize", "Serialize", "TrySerialize" };
            if (!abstractType)
                names.UnionWith(new[] { "BinaryCodec", "GetBinaryCodec", "TryDeserialize" });
            foreach (var member in type.GetMembers().Where(m => names.Contains(m.Name) && !m.IsImplicitlyDeclared))
            {
                Report(ReservedMember, member, type.Name, member.Name);
                return true;
            }
            return false;
        }

        private static bool HasStoredState(INamedTypeSymbol type)
        {
            for (var current = type; current is { SpecialType: not SpecialType.System_Object }; current = current.BaseType)
                if (current.GetMembers().Any(member => member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false }
                    || member is IPropertySymbol { IsStatic: false, IsIndexer: false } property && IsAutoProperty(property)))
                    return true;
            return false;
        }

        private IEnumerable<ISymbol> GetStorageMembers(INamedTypeSymbol type)
        {
            foreach (var member in type.GetMembers())
            {
                if (member is IFieldSymbol { IsStatic: false, IsConst: false, IsImplicitlyDeclared: false } field)
                    yield return field;
                else if (member is IPropertySymbol { IsStatic: false, IsIndexer: false } property && IsAutoProperty(property))
                    yield return property;
                else if (member is IPropertySymbol property2 && HasRepresentation(property2))
                    FailMember(property2, "只有自动实现属性具有可序列化存储");
            }
        }

        private static bool IsAutoProperty(IPropertySymbol property)
            => property.DeclaringSyntaxReferences.Any(r => r.GetSyntax() is PropertyDeclarationSyntax p
                && p.ExpressionBody is null && p.AccessorList is not null
                && p.AccessorList.Accessors.All(a => a.Body is null && a.ExpressionBody is null));

        private AttributeData? GetAttribute(ISymbol symbol, string name)
        {
            var direct = symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == name);
            if (direct is not null) return direct;
            if (symbol is IPropertySymbol property)
                return property.ContainingType.GetMembers().OfType<IFieldSymbol>()
                    .FirstOrDefault(f => SymbolEqualityComparer.Default.Equals(f.AssociatedSymbol, property))?
                    .GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == name);
            return null;
        }

        private bool Has(ISymbol symbol, string name) => GetAttribute(symbol, name) is not null;
        private bool HasRepresentation(ISymbol symbol)
            => Has(symbol, BinaryMemberName)
                || Has(symbol, BinaryFieldOffsetName)
                || Has(symbol, BinaryFixedArrayName)
                || Has(symbol, BinaryDataTypeName);
        private int? GetEndian(ISymbol symbol) => GetAttribute(symbol, EndianName) is { } a ? GetCtorInt(a) : null;
        private int? GetEndian(ITypeSymbol symbol) => symbol.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == EndianName) is { } a ? GetCtorInt(a) : null;
        private int? GetInheritedEndian(INamedTypeSymbol type)
        { for (var current = type; current is not null; current = current.BaseType) { var value = GetEndian(current); if (value is not null) return value; } return null; }

        private static LayoutKindValue? GetLayout(INamedTypeSymbol type, out int size, out int inclusion)
        {
            size = inclusion = 0;
            var attribute = type.GetAttributes().FirstOrDefault(
                candidate => candidate.AttributeClass?.ToDisplayString() == FixedBinarySerializableName);
            if (attribute is null)
                return null;
            size = GetNamedInt(attribute, "Size") ?? 0;
            inclusion = GetNamedInt(attribute, "MemberInclusion") ?? 0;
            if (inclusion is not 0 and not 1)
                return null;
            return GetCtorInt(attribute) switch { 0 => LayoutKindValue.Sequential, 1 => LayoutKindValue.Explicit, _ => null };
        }

        private static int GetCtorInt(AttributeData data) => data.ConstructorArguments.Length == 0 ? 0 : Convert.ToInt32(data.ConstructorArguments[0].Value);
        private static int? GetNamedInt(AttributeData data, string name)
            => data.NamedArguments.FirstOrDefault(p => p.Key == name).Value.Value is { } value ? Convert.ToInt32(value) : null;
        private static int Align(int value, int alignment) => checked((value + alignment - 1) / alignment * alignment);
        private static bool HasGenericContainer(INamedTypeSymbol type)
        { for (var current = type.ContainingType; current is not null; current = current.ContainingType) if (current.Arity != 0) return true; return false; }
        private static SyntaxNode? GetDeclaration(ISymbol symbol) => symbol.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax()?.FirstAncestorOrSelf<TypeDeclarationSyntax>();
        private static int SourceOrder(ISymbol symbol) => symbol.DeclaringSyntaxReferences.FirstOrDefault()?.Span.Start ?? int.MaxValue;
        private static Location Location(ISymbol symbol) => symbol.Locations.FirstOrDefault(l => l.IsInSource) ?? Microsoft.CodeAnalysis.Location.None;

        private TypePlan? FailType(INamedTypeSymbol type, string reason) { Report(InvalidType, type, type.Name, reason); return null; }
        private TypePlan? FailLayout(INamedTypeSymbol type, string reason) { Report(InvalidLayout, type, type.Name, reason); return null; }
        private MemberPlan? FailMember(ISymbol member, string reason) { Report(InvalidMember, member, member.Name, reason); return null; }
        private void Report(DiagnosticDescriptor descriptor, ISymbol symbol, params object[] args)
        { HasErrors = descriptor.DefaultSeverity == DiagnosticSeverity.Error || HasErrors; _output.ReportDiagnostic(Diagnostic.Create(descriptor, Location(symbol), args)); }
    }

    private static class Emitter
    {
        private const string Serialization = "global::Lytec.Common.Serialization.";
        private const string Endian = "global::Lytec.Common.Data.Endian";

        public static string Emit(TypePlan plan)
        {
            var sb = new StringBuilder();
            sb.AppendLine("// <auto-generated />").AppendLine("#nullable enable");
            var closing = OpenContainers(sb, plan.Type);
            EmitBody(sb, plan);
            for (var i = 0; i < closing; i++) sb.AppendLine("}");
            sb.AppendLine("#nullable restore");
            return CSharpSyntaxTree.ParseText(sb.ToString()).GetRoot().NormalizeWhitespace().ToFullString();
        }

        private static void EmitBody(StringBuilder sb, TypePlan plan)
        {
            var typeName = Name(plan.Type);
            var modifier = plan.Type.TypeKind == TypeKind.Struct && plan.Type.IsReadOnly ? "readonly " : "";
            sb.Append("partial ").Append(modifier).Append(plan.Type.TypeKind == TypeKind.Struct ? "struct " : "class ").Append(Escape(plan.Type.Name));
            if (!Implements(plan.Type, MarkerName))
                sb.Append(" : ").Append(Serialization).Append("IBinarySerializable");
            sb.AppendLine();
            sb.AppendLine("{");
            foreach (var member in plan.Members.Where(member => member.CodecProvider is not null))
                sb.Append("private static readonly ").Append(Name(member.CodecProvider!)).Append(' ')
                    .Append(ProviderField(member)).Append(" = new ").Append(Name(member.CodecProvider!)).AppendLine("();");
            var isOverride = plan.Base is not null;
            var dispatch = plan.Type.TypeKind == TypeKind.Class ? (isOverride ? "override " : "virtual ") : "";
            sb.Append("public ").Append(dispatch).Append("int SerializedSize => ").Append(plan.Size).AppendLine(";");
            sb.Append("public ").Append(dispatch).AppendLine("byte[] Serialize(global::Lytec.Common.Data.Endian? endian = null)");
            sb.AppendLine("{").Append("var result = new byte[").Append(plan.Size).AppendLine("];")
                .AppendLine("var status = TrySerialize(result, out var written, endian);")
                .AppendLine("if (status != global::System.Buffers.OperationStatus.Done || written != result.Length)")
                .AppendLine("throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"ValueNotSerializable\", \"当前对象不能序列化为声明的固定二进制布局。\");")
                .AppendLine("return result;").AppendLine("}");
            sb.Append("public ").Append(dispatch).AppendLine("global::System.Buffers.OperationStatus TrySerialize(global::System.Span<byte> destination, out int written, global::Lytec.Common.Data.Endian? endian = null)")
                .AppendLine("{").AppendLine("written = 0;")
                .Append("if (destination.Length < ").Append(plan.Size).AppendLine(") return global::System.Buffers.OperationStatus.DestinationTooSmall;")
                .Append("var temporary = new byte[").Append(plan.Size).AppendLine("];")
                .Append("if (!").Append(WriteMethod(plan)).AppendLine("(temporary, endian)) return global::System.Buffers.OperationStatus.InvalidData;")
                .AppendLine("((global::System.ReadOnlySpan<byte>)temporary).CopyTo(destination);").Append("written = ").Append(plan.Size).AppendLine(";")
                .AppendLine("return global::System.Buffers.OperationStatus.Done;").AppendLine("}");

            EmitWriter(sb, plan);
            EmitConstructor(sb, plan);

            if (!plan.IsAbstract)
                EmitConcreteApi(sb, plan, typeName);
            sb.AppendLine("}");
        }

        private static void EmitWriter(StringBuilder sb, TypePlan plan)
        {
            var access = plan.Type.TypeKind == TypeKind.Class ? "protected" : "private";
            sb.Append(access).Append(" bool ").Append(WriteMethod(plan)).AppendLine("(global::System.Span<byte> destination, global::Lytec.Common.Data.Endian? endian)")
                .AppendLine("{")
                .AppendLine("var __resolvedEndian = global::Lytec.Common.Serialization.FixedBinaryPrimitives.ResolveEndian(endian);");
            if (plan.Base is not null)
                sb.Append("if (!base.").Append(WriteMethod(plan.Base)).AppendLine("(destination, endian)) return false;");
            foreach (var member in plan.Members)
                EmitWriteMember(sb, member, "this." + Escape(member.Name), member.Offset, "__resolvedEndian");
            sb.AppendLine("return true;").AppendLine("}");
        }

        private static void EmitConstructor(StringBuilder sb, TypePlan plan)
        {
            var access = plan.Type.TypeKind == TypeKind.Class ? "protected" : "private";
            sb.Append(access).Append(' ').Append(Escape(plan.Type.Name))
                .AppendLine("(global::System.ReadOnlySpan<byte> source, global::Lytec.Common.Data.Endian? endian, int __binaryConstructorMarker)");
            if (plan.Base is not null)
                sb.Append(" : base(source.Slice(0, ").Append(plan.Base.Size).AppendLine("), endian, 0)");
            else if (plan.Type.TypeKind == TypeKind.Struct)
                sb.AppendLine(" : this()");
            sb.AppendLine("{").AppendLine("var __resolvedEndian = global::Lytec.Common.Serialization.FixedBinaryPrimitives.ResolveEndian(endian);");
            foreach (var member in plan.Members)
                EmitReadMember(sb, member, "this." + Escape(member.Name), member.Offset, "__resolvedEndian");
            sb.AppendLine("}");
        }

        private static void EmitConcreteApi(StringBuilder sb, TypePlan plan, string typeName)
        {
            var hidesBase = plan.Base is null ? "" : "new ";
            sb.Append("private static readonly ").Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine("> __binaryDefaultCodec = new __BinaryCodec(null);")
                .Append("private static readonly ").Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine("> __binaryLittleCodec = new __BinaryCodec(global::Lytec.Common.Data.Endian.Little);")
                .Append("private static readonly ").Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine("> __binaryBigCodec = new __BinaryCodec(global::Lytec.Common.Data.Endian.Big);")
                .Append("public static ").Append(hidesBase).Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine("> BinaryCodec => __binaryDefaultCodec;")
                .Append("public static ").Append(hidesBase).Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine("> GetBinaryCodec(global::Lytec.Common.Data.Endian? endian = null)")
                .AppendLine("{").AppendLine("if (endian is null) return __binaryDefaultCodec;")
                .AppendLine("if (endian == global::Lytec.Common.Data.Endian.Little) return __binaryLittleCodec;")
                .AppendLine("if (endian == global::Lytec.Common.Data.Endian.Big) return __binaryBigCodec;")
                .AppendLine("throw new global::System.ArgumentOutOfRangeException(nameof(endian));").AppendLine("}")
                .Append("public static global::System.Buffers.OperationStatus TryDeserialize(global::System.ReadOnlySpan<byte> source, out ").Append(typeName).AppendLine(" value, global::Lytec.Common.Data.Endian? endian = null)")
                .AppendLine("{").Append("if (source.Length != ").Append(plan.Size).AppendLine(") { value = default!; return global::System.Buffers.OperationStatus.InvalidData; }")
                .AppendLine("var parsed = GetBinaryCodec(endian).Parse(source, true, out value);")
                .AppendLine("return parsed.Status;").AppendLine("}");

            sb.Append("private sealed class __BinaryCodec : ").Append(Serialization).Append("IFixedBinaryCodec<").Append(typeName).AppendLine(">")
                .AppendLine("{").AppendLine("private readonly global::Lytec.Common.Data.Endian? _endian;")
                .AppendLine("public __BinaryCodec(global::Lytec.Common.Data.Endian? endian) => _endian = endian;")
                .Append("public int FixedSize => ").Append(plan.Size).AppendLine(";")
                .Append("public int MinimumFrameLength => ").Append(plan.Size).AppendLine(";")
                .Append("public int MaximumFrameLength => ").Append(plan.Size).AppendLine(";")
                .Append("public ").Append(Serialization).AppendLine("BinaryResynchronizationMode ResynchronizationMode => global::Lytec.Common.Serialization.BinaryResynchronizationMode.StopOnInvalidData;")
                .Append("public global::System.Buffers.OperationStatus TryGetSerializedLength(").Append(typeName).AppendLine(" value, out int length)")
                .AppendLine("{");
            if (plan.Type.IsReferenceType)
                sb.AppendLine("if (value is null) { length = 0; return global::System.Buffers.OperationStatus.InvalidData; }");
            sb.Append("if (value.SerializedSize != ").Append(plan.Size).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"RuntimeTypeSizeMismatch\", \"运行期派生类型的固定大小与声明槽位不一致。\");")
                .Append("var temporary = new byte[").Append(plan.Size).AppendLine("];")
                .AppendLine("var status = value.TrySerialize(temporary, out var written, _endian);")
                .Append("if (status == global::System.Buffers.OperationStatus.Done && written == ").Append(plan.Size).AppendLine(") { length = written; return status; }")
                .AppendLine("length = 0; return global::System.Buffers.OperationStatus.InvalidData;").AppendLine("}")
                .Append("public global::System.Buffers.OperationStatus TrySerialize(").Append(typeName).AppendLine(" value, global::System.Span<byte> destination, out int written)")
                .AppendLine("{");
            if (plan.Type.IsReferenceType)
                sb.AppendLine("if (value is null) { written = 0; return global::System.Buffers.OperationStatus.InvalidData; }");
            sb.Append("if (value.SerializedSize != ").Append(plan.Size).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"RuntimeTypeSizeMismatch\", \"运行期派生类型的固定大小与声明槽位不一致。\");")
                .AppendLine("return value.TrySerialize(destination, out written, _endian);").AppendLine("}")
                .Append("public ").Append(Serialization).Append("BinaryParseResult Parse(global::System.ReadOnlySpan<byte> source, bool isFinalBlock, out ").Append(typeName).AppendLine(" value)")
                .AppendLine("{").Append("if (source.Length < ").Append(plan.Size).AppendLine(") { value = default!; return isFinalBlock ? global::Lytec.Common.Serialization.BinaryParseResult.InvalidData(0, source.Length) : global::Lytec.Common.Serialization.BinaryParseResult.NeedMoreData(source.Length); }")
                .Append("value = new ").Append(typeName).AppendLine("(source.Slice(0, FixedSize), _endian, 0);")
                .AppendLine("return global::Lytec.Common.Serialization.BinaryParseResult.Done(FixedSize);").AppendLine("}")
                .Append("public ").Append(Serialization).Append("IBinaryStreamDecoder<").Append(typeName).AppendLine("> CreateStreamDecoder() => new global::Lytec.Common.Serialization.FixedBinaryStreamDecoder<" + typeName + ">(this);")
                .AppendLine("}");
        }

        private static void EmitWriteMember(StringBuilder sb, MemberPlan m, string value, int offset, string endian)
        {
            var e = EndianExpression(m, endian);
            if (m.Kind == DataKind.Array)
            {
                sb.Append("if (").Append(value).AppendLine(" is null) return false;")
                    .Append("if (").Append(value).Append(".Length > ").Append(m.ElementCount).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidArgument(\"ArrayLongerThanFixedLength\", \"数组长度不能大于 BinaryFixedArray.Length。\");")
                    .Append("for (var __i = 0; __i < ").Append(value).AppendLine(".Length; __i++)").AppendLine("{");
                EmitWriteValue(sb, m.ElementKind, m.ElementType!, value + "[__i]", $"{offset} + __i * {m.ElementSize}", m.ElementSize, e, m.CodecProvider is null ? null : ProviderField(m));
                sb.AppendLine("}"); return;
            }
            EmitWriteValue(sb, m.Kind, m.Type, value, offset.ToString(), m.Size, e, m.CodecProvider is null ? null : ProviderField(m));
        }

        private static void EmitWriteValue(
            StringBuilder sb,
            DataKind kind,
            ITypeSymbol type,
            string value,
            string offset,
            int size,
            string endian,
            string? codecProvider)
        {
            if (kind == DataKind.Nested)
            {
                sb.AppendLine("{");
                if (type.IsReferenceType) sb.Append("if (").Append(value).AppendLine(" is null) return false;");
                sb.Append("if (((global::Lytec.Common.Serialization.IBinarySerializable)").Append(value).Append(").SerializedSize != ").Append(size).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"RuntimeTypeSizeMismatch\", \"运行期派生类型的固定大小与声明槽位不一致。\");");
                if (codecProvider is null)
                    sb.Append("var __status = ((global::Lytec.Common.Serialization.IBinarySerializable)").Append(value).Append(").TrySerialize(destination.Slice(").Append(offset).Append(", ").Append(size).AppendLine("), out var __written, " + endian + ");");
                else
                    sb.Append("var __codec = ((").Append(Serialization).Append("IFixedBinaryCodecProvider<").Append(Name(type)).Append(">)")
                        .Append(codecProvider).Append(").GetBinaryCodec(").Append(endian).AppendLine(");")
                        .Append("if (__codec.FixedSize != ").Append(size).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"ManualContractSizeMismatch\", \"手动固定二进制契约的 codec 大小与声明大小不一致。\");")
                        .Append("var __status = __codec.TrySerialize(").Append(value).Append(", destination.Slice(").Append(offset).Append(", ").Append(size).AppendLine("), out var __written);");
                sb
                    .Append("if (__status != global::System.Buffers.OperationStatus.Done || __written != ").Append(size).AppendLine(") return false;")
                    .AppendLine("}");
                return;
            }
            var converted = ConvertWrite(type, kind, value);
            switch (kind)
            {
                case DataKind.I1: sb.Append("destination[").Append(offset).Append("] = unchecked((byte)").Append(converted).AppendLine(");"); break;
                case DataKind.U1: sb.Append("destination[").Append(offset).Append("] = unchecked((byte)").Append(converted).AppendLine(");"); break;
                case DataKind.I2: Call("WriteInt16"); break;
                case DataKind.U2: Call("WriteUInt16"); break;
                case DataKind.I4: case DataKind.Bool4: Call("WriteInt32"); break;
                case DataKind.U4: Call("WriteUInt32"); break;
                case DataKind.I8: Call("WriteInt64"); break;
                case DataKind.U8: Call("WriteUInt64"); break;
                case DataKind.R4: Call("WriteSingle"); break;
                case DataKind.R8: Call("WriteDouble"); break;
            }
            void Call(string method) => sb.Append(Serialization).Append("FixedBinaryPrimitives.").Append(method).Append("(destination.Slice(").Append(offset).Append(", ").Append(size).Append("), ").Append(converted).Append(", ").Append(endian).AppendLine(");");
        }

        private static void EmitReadMember(StringBuilder sb, MemberPlan m, string target, int offset, string endian)
        {
            var e = EndianExpression(m, endian);
            if (m.Kind == DataKind.Array)
            {
                var elementName = Name(m.ElementType!);
                sb.Append("var __array_").Append(Safe(m.Name)).Append(" = new ").Append(elementName).Append('[').Append(m.ElementCount).AppendLine("];")
                    .Append("for (var __i = 0; __i < ").Append(m.ElementCount).AppendLine("; __i++)").AppendLine("{");
                EmitReadValue(sb, m.ElementKind, m.ElementType!, $"__array_{Safe(m.Name)}[__i]", $"{offset} + __i * {m.ElementSize}", m.ElementSize, e, m.CodecProvider is null ? null : ProviderField(m));
                sb.AppendLine("}").Append(target).Append(" = __array_").Append(Safe(m.Name)).AppendLine(";"); return;
            }
            EmitReadValue(sb, m.Kind, m.Type, target, offset.ToString(), m.Size, e, m.CodecProvider is null ? null : ProviderField(m));
        }

        private static void EmitReadValue(
            StringBuilder sb,
            DataKind kind,
            ITypeSymbol type,
            string target,
            string offset,
            int size,
            string endian,
            string? codecProvider)
        {
            if (kind == DataKind.Nested)
            {
                sb.AppendLine("{").Append("var __codec = ");
                if (codecProvider is null)
                    sb.Append(Name(type)).Append(".GetBinaryCodec(").Append(endian).AppendLine(");");
                else
                    sb.Append("((").Append(Serialization).Append("IFixedBinaryCodecProvider<").Append(Name(type)).Append(">)")
                        .Append(codecProvider).Append(").GetBinaryCodec(").Append(endian).AppendLine(");")
                        .Append("if (__codec.FixedSize != ").Append(size).AppendLine(") throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"ManualContractSizeMismatch\", \"手动固定二进制契约的 codec 大小与声明大小不一致。\");");
                sb.Append("var __parsed = __codec.Parse(source.Slice(").Append(offset).Append(", ").Append(size).AppendLine("), true, out var __nested);")
                    .AppendLine("if (__parsed.Status != global::System.Buffers.OperationStatus.Done) throw global::Lytec.Common.Serialization.BinarySerializationExceptionFactory.InvalidOperation(\"ValidatedNestedValueFailed\", \"已经验证的内联对象无法反序列化。\");")
                    .Append(target).AppendLine(" = __nested;").AppendLine("}"); return;
            }
            string read = kind switch
            {
                DataKind.I1 => $"unchecked((sbyte)source[{offset}])", DataKind.U1 => $"source[{offset}]",
                DataKind.I2 => Read("ReadInt16"), DataKind.U2 => Read("ReadUInt16"),
                DataKind.I4 or DataKind.Bool4 => Read("ReadInt32"), DataKind.U4 => Read("ReadUInt32"),
                DataKind.I8 => Read("ReadInt64"), DataKind.U8 => Read("ReadUInt64"),
                DataKind.R4 => Read("ReadSingle"), DataKind.R8 => Read("ReadDouble"),
                _ => "default"
            };
            var expression = ConvertRead(type, kind, read);
            sb.Append(target).Append(" = ").Append(expression).AppendLine(";");
            string Read(string method) => $"{Serialization}FixedBinaryPrimitives.{method}(source.Slice({offset}, {size}), {endian})";
        }

        private static string ConvertWrite(ITypeSymbol type, DataKind kind, string value)
        {
            if (type.SpecialType == SpecialType.System_Boolean)
            {
                var booleanCast = kind switch
                {
                    DataKind.I1 => "sbyte",
                    DataKind.U1 => "byte",
                    DataKind.I2 => "short",
                    DataKind.U2 => "ushort",
                    DataKind.I4 or DataKind.Bool4 => "int",
                    DataKind.U4 => "uint",
                    _ => "int",
                };
                return $"({value} ? ({booleanCast})1 : ({booleanCast})0)";
            }
            var cast = kind switch { DataKind.I1 => "sbyte", DataKind.U1 => "byte", DataKind.I2 => "short", DataKind.U2 => "ushort", DataKind.I4 or DataKind.Bool4 => "int", DataKind.U4 => "uint", DataKind.I8 => "long", DataKind.U8 => "ulong", DataKind.R4 => "float", DataKind.R8 => "double", _ => "int" };
            return $"unchecked(({cast})({value}))";
        }

        private static string ConvertRead(ITypeSymbol type, DataKind kind, string read)
        {
            if (type.SpecialType == SpecialType.System_Boolean)
                return $"({read}) != 0";
            return $"unchecked(({Name(type)})({read}))";
        }

        private static string EndianExpression(MemberPlan member, string fallback)
            => member.FixedEndian switch { 0 => Endian + ".Little", 1 => Endian + ".Big", _ => fallback };
        private static string ProviderField(MemberPlan member) => "__binaryProvider_" + Safe(member.Name);
        private static string WriteMethod(TypePlan plan) => "__BinaryWrite_" + Safe(plan.Type.Name);
        private static string Name(ITypeSymbol type) => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        private static string Escape(string name) => SyntaxFacts.GetKeywordKind(name) == SyntaxKind.None ? name : "@" + name;
        private static string Safe(string name) => new(name.Select(c => char.IsLetterOrDigit(c) ? c : '_').ToArray());

        private static int OpenContainers(StringBuilder sb, INamedTypeSymbol type)
        {
            var stack = new Stack<INamedTypeSymbol>();
            for (var current = type.ContainingType; current is not null; current = current.ContainingType) stack.Push(current);
            var count = 0;
            if (!type.ContainingNamespace.IsGlobalNamespace)
            { sb.Append("namespace ").Append(type.ContainingNamespace.ToDisplayString()).AppendLine().AppendLine("{"); count++; }
            foreach (var container in stack)
            {
                sb.Append("partial ");
                if (container.TypeKind == TypeKind.Struct && container.IsReadOnly) sb.Append("readonly ");
                sb.Append(container.TypeKind == TypeKind.Struct ? "struct " : "class ").Append(Escape(container.Name)).AppendLine().AppendLine("{"); count++;
            }
            return count;
        }
    }
}
