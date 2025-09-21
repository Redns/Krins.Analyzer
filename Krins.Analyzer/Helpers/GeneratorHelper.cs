using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Krins.Analyzer.Helpers
{
    public static class GeneratorHelper
    {
        /// <summary>
        /// 获取访问权限的字面量
        /// </summary>
        /// <param name="accessibility"></param>
        /// <returns></returns>
        public static string GetAccessibilityLiteral(this Accessibility accessibility) =>
            accessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "protected internal",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.Public => "public",
                _ => string.Empty,
            };

        /// <summary>
        /// 获取类型最简化名称
        /// </summary>
        /// <param name="typeSymbol"></param>
        /// <returns></returns>
        public static string GetFullyQualifiedName(this ITypeSymbol typeSymbol)
        {
            if (typeSymbol is IArrayTypeSymbol arrayTypeSymbol)
            {
                var elementType = GetFullyQualifiedName(arrayTypeSymbol.ElementType);
                return $"{elementType}[]";
            }
            else if (typeSymbol is INamedTypeSymbol namedTypeSymbol)
            {
                return namedTypeSymbol.ToDisplayString().Split('.').Last();
            }
            else
            {
                return typeSymbol.ToString();
            }
        }

        /// <summary>
        /// 获取类的 using 指令
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public static List<string> GetUsingDirectives(this SyntaxNode node)
        {
            var usingDirectives = new List<string>();
            var compilationUnit = node.AncestorsAndSelf()
                .OfType<CompilationUnitSyntax>()
                .FirstOrDefault();
            if (compilationUnit != null)
            {
                foreach (var usingDirective in compilationUnit.Usings)
                {
                    usingDirectives.Add(usingDirective.ToString());
                }
            }
            return usingDirectives;
        }
    }
}
