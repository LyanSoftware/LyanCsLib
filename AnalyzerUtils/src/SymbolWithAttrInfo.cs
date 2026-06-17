using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace Lytec.Analyzer;

public record SymbolWithAttrInfo(
    ISymbol Symbol,
    ImmutableArray<AttributeData> Attrs
);
