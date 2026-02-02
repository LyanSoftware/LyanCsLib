using Microsoft.CodeAnalysis;
using Org.BouncyCastle.Crypto.Tls;

namespace Lytec.Analyzer;

public static class SymbolUtils
{
    public static bool IsClass(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Class;

    public static bool IsStruct(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Struct;

    public static bool IsInterface(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Interface;

    public static bool IsDelegate(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Delegate;

    public static bool IsEnum(this INamedTypeSymbol symbol) => symbol.TypeKind == TypeKind.Enum;

    public static bool IsEqualsTo(this ISymbol? a, ISymbol? b) => SymbolEqualityComparer.Default.Equals(a, b);

    public static string GetFullName(this INamedTypeSymbol symbol)
    {
        const string globalns = "global::";
        var name = symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        return name.StartsWith(globalns) ? name[globalns.Length..] : name;
    }

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
        return -1;
    }

    public static bool IsPointer(this ITypeSymbol symbol) => symbol.TypeKind == TypeKind.Pointer;

    public static string GetTypeKeyword(this INamedTypeSymbol symbol)
    {
        if (symbol.IsRecord)
            return symbol.IsValueType ? "record struct" : "record";
        return symbol.TypeKind.ToString().ToLowerInvariant();
    }

    public static bool HasNoArgsConstructor(this INamedTypeSymbol t, bool onlyPublic = false)
    {
        if (t.IsStruct())
            return true;

        if (t.IsInterface() ||
            t.IsDelegate() ||
            t.IsEnum() ||
            t.IsStatic)
        {
            return false;
        }

        var cs = t.Constructors;

        if (cs.IsEmpty)
        {
            if (!t.IsRecord)
                return true;
            return !t.GetMembers()
                .OfType<IPropertySymbol>()
                .Any(m => m.DeclaredAccessibility == Accessibility.Public && m.IsImplicitlyDeclared);
        }
        else
        {
            foreach (var constructor in cs)
            {
                // 如果要求仅公共构造函数，检查可访问性
                if (onlyPublic && constructor.DeclaredAccessibility != Accessibility.Public)
                    continue;

                if (constructor.Parameters.Length == 0)
                    return true;
            }
            return false;
        }
    }
}
