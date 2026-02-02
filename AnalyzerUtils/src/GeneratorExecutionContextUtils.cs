using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Lytec.Analyzer;

public static class GeneratorExecutionContextUtils
{
    public static INamedTypeSymbol? GetTypeSymbol(this GeneratorExecutionContext context, string fullname)
    => context.Compilation.GetTypeByMetadataName(fullname);

    public static INamedTypeSymbol? GetTypeSymbol(this GeneratorExecutionContext context, Type type)
    => context.Compilation.GetTypeByMetadataName(type.FullName);
    
    public static INamedTypeSymbol? GetTypeSymbol<T>(this GeneratorExecutionContext context)
    => context.Compilation.GetTypeByMetadataName(typeof(T).FullName);

}
