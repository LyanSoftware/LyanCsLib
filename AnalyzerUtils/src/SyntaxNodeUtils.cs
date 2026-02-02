using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Lytec.Analyzer;

public static class SyntaxNodeUtils
{
    public static bool IsPartialType(this SyntaxNode node) => node is TypeDeclarationSyntax td && td.Modifiers.Any(SyntaxKind.PartialKeyword);
}
