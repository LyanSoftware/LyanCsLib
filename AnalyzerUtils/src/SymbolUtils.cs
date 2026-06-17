using Microsoft.CodeAnalysis;

namespace Lytec.Analyzer;

public static class SymbolUtils
{
    public static bool IsClass(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Class;

    public static bool IsStruct(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Struct;

    public static bool IsInterface(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Interface;

    public static bool IsDelegate(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Delegate;

    public static bool IsEnum(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Enum;

    public static bool IsEqualsTo(this ISymbol? a, ISymbol? b) => SymbolEqualityComparer.Default.Equals(a, b);

    public static string GetFiltedMetadataName(this INamedTypeSymbol type)
    => type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat)
        .Replace("global::", "")
        .Replace('<', '[')
        .Replace('>', ']')
        .Replace(" ", "");

    public static string GetFullyQualifiedString(this ISymbol symbol) => symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

    public static string GetFullName(this ISymbol symbol)
    {
        const string globalns = "global::";
        var name = symbol.GetFullyQualifiedString();
        return name.StartsWith(globalns) ? name[globalns.Length..] : name;
    }

    public static bool HasAttr(this ISymbol symbol, string name) => symbol.GetAttributes().Any(a => a.AttributeClass?.GetFullName() == name);
    public static bool HasAttr(this ISymbol symbol, Type type) => symbol.HasAttr(type.FullName);
    public static bool HasAttr<T>(this ISymbol symbol) where T : Attribute => symbol.HasAttr(typeof(T));

    public static bool HasAttr(this ISymbol symbol, string name, Compilation compilation)
    => symbol.GetAttributes().Any(attr => SymbolEqualityComparer.Default.Equals(attr.AttributeClass, compilation.GetTypeByMetadataName(name)));
    public static bool HasAttr(this ISymbol symbol, Type type, Compilation compilation) => symbol.HasAttr(type.FullName, compilation);
    public static bool HasAttr<T>(this ISymbol symbol, Compilation compilation) where T : Attribute => symbol.HasAttr(typeof(T), compilation);

    public static bool IsSubtypeOf(this INamedTypeSymbol symbol, INamedTypeSymbol type)
    {
        INamedTypeSymbol? symb = symbol;
        while (symb != null)
        {
            if (type.IsInterface())
            {
                if (type.IsGenericType)
                {
                    if (symb.AllInterfaces.Any(i => i.OriginalDefinition.IsEqualsTo(type)))
                        return true;
                }
                else
                {
                    if (symb.IsEqualsTo(type))
                        return true;
                    if (symb.AllInterfaces.Any(i => SymbolEqualityComparer.Default.Equals(i, type)))
                        return true;
                }
            }
            else
            {
                if (SymbolEqualityComparer.Default.Equals(symb, type))
                    return true;
            }
            symb = symb.BaseType;
        }
        return false;
    }

    public static bool IsPrimitiveOrEnum(this ITypeSymbol symbol)
    {
        switch (symbol.SpecialType)
        {
            case SpecialType.System_Enum:
            case SpecialType.System_Boolean:
            case SpecialType.System_Char:
            case SpecialType.System_SByte:
            case SpecialType.System_Byte:
            case SpecialType.System_Int16:
            case SpecialType.System_UInt16:
            case SpecialType.System_Int32:
            case SpecialType.System_UInt32:
            case SpecialType.System_Int64:
            case SpecialType.System_UInt64:
            case SpecialType.System_Single:
            case SpecialType.System_Double:
                return true;
            default:
                return false;
        }
    }

    public static int GetPrimitiveSize(this ITypeSymbol symbol)
    {
        switch (symbol.SpecialType)
        {
            case SpecialType.System_Enum:
                if (symbol is INamedTypeSymbol nts && nts.EnumUnderlyingType != null)
                    return nts.EnumUnderlyingType.GetPrimitiveSize();
                break;
            case SpecialType.System_Boolean:
                return sizeof(bool);
            case SpecialType.System_Char:
                return sizeof(char);
            case SpecialType.System_SByte:
                return sizeof(sbyte);
            case SpecialType.System_Byte:
                return sizeof(byte);
            case SpecialType.System_Int16:
                return sizeof(short);
            case SpecialType.System_UInt16:
                return sizeof(ushort);
            case SpecialType.System_Int32:
                return sizeof(int);
            case SpecialType.System_UInt32:
                return sizeof(uint);
            case SpecialType.System_Int64:
                return sizeof(long);
            case SpecialType.System_UInt64:
                return sizeof(ulong);
            case SpecialType.System_Single:
                return sizeof(float);
            case SpecialType.System_Double:
                return sizeof(double);
        }
        throw new NotSupportedException();
    }

    public static bool IsPointer(this ITypeSymbol symbol) => symbol.TypeKind == TypeKind.Pointer;

    public static string GetTypeKeyword(this INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
            return symbol.IsValueType ? "record struct" : "record";
        return symbol.TypeKind.ToString().ToLowerInvariant();
    }

    /// <summary>
    /// 判断类型是否具有可访问的无参构造函数。
    /// </summary>
    /// <param name="type">要检查的类型符号</param>
    /// <param name="minAccessibility">最低允许的访问级别，默认为 Public。
    /// 如果为 NotApplicable，表示不限制访问性，只要存在无参构造即可。</param>
    /// <returns>存在满足条件的无参构造返回 true，否则 false。</returns>
    public static bool HasNoArgsConstructor(this INamedTypeSymbol t, Accessibility minAccessibility = Accessibility.Public)
    {
        if (t.IsInterface() ||
            t.IsDelegate() ||
            t.IsEnum() ||
            t.IsStatic)
        {
            return false;
        }

        // 如果没有显式定义任何构造函数，编译器会生成隐式无参构造
        // 但仅对 class、struct、record class、record struct 有效
        // 且隐式构造函数始终是 public
        if (!t.Constructors.Any())
            return true;

        // 查找显式定义的无参构造函数
        var ctor = t.Constructors.FirstOrDefault(c => c.Parameters.Length == 0);

        if (ctor != null)
        {
            // 检查该构造函数的可访问性是否满足最低要求
            return IsAccessible(ctor.DeclaredAccessibility, minAccessibility);
        }

        // 存在其他带参数的构造函数，但没有无参构造
        return false;
    }

    /// <summary>
    /// 判断一个访问级别是否满足最低要求。
    /// </summary>
    public static bool IsAccessible(this Accessibility accessibility, Accessibility min)
    {
        switch (min)
        {
            case Accessibility.NotApplicable:
                return true;
            // 根据常见需求，仅支持 Public 和 Internal 两种门槛
            // 如果需要支持更多级别（Protected、Private），可扩展此方法
            case Accessibility.Public:
                return accessibility == Accessibility.Public;
            case Accessibility.Internal:
                return accessibility == Accessibility.Public || accessibility == Accessibility.Internal;
            default:
                // 其他未处理的情况默认返回 false
                return false;
        }
    }

    public static IEnumerable<string> GetConstraints(this ITypeParameterSymbol typeParameter)
    {
        if (typeParameter.HasNotNullConstraint)
            yield return "notnull";

        if (typeParameter.HasReferenceTypeConstraint)
            yield return "class";

        if (typeParameter.HasUnmanagedTypeConstraint)
            yield return "unmanaged";
        else if (typeParameter.HasValueTypeConstraint)
            yield return "struct";

        foreach (var constraintType in typeParameter.ConstraintTypes)
            yield return constraintType.GetFullyQualifiedString();

        if (typeParameter.HasConstructorConstraint)
            yield return "new()";
    }

}
