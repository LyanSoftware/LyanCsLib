using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using Lytec.Common;

namespace Lytec.Analyzer;

public static class SyntaxProviderUtils
{
    public static IncrementalValueProvider<ImmutableArray<SymbolWithAttrInfo>> ForAttr<TAttr>(
        this SyntaxValueProvider provider,
        Func<SyntaxNode, bool> predicate
        )
        where TAttr : Attribute
    {
        return provider.ForAttributeWithMetadataName(
        typeof(TAttr).FullName,
        (node, _) => predicate(node),
        (syntaxContext, _) => (
            syntaxContext.TargetSymbol,
            syntaxContext.Attributes
            )
        )
        .Collect()
        .Select((items, _) => items.GroupBy(x => x.TargetSymbol, SymbolEqualityComparer.Default)
            .Select(g => new SymbolWithAttrInfo(
                g.Key,
                g.SelectMany(x => x.Attributes)
                 .ToImmutableArray())
            )
            .ToImmutableArray()
        );
    }

}
