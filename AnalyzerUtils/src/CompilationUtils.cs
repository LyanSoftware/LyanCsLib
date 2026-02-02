using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;

namespace Lytec.Analyzer;

public static class CompilationUtils
{
    public static INamedTypeSymbol? GetTypeSymbol(this Compilation compilation, Type type)
    => compilation.GetTypeByMetadataName(type.FullName);

    public static INamedTypeSymbol? GetTypeSymbol<T>(this Compilation compilation)
    => compilation.GetTypeByMetadataName(typeof(T).FullName);

}
