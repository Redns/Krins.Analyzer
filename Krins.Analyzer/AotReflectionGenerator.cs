using System.Linq;
using System.Text;
using Krins.Analyzer.Helpers;
using Microsoft.CodeAnalysis;

namespace Krins.Analyzer
{
    [Generator]
    public class AotReflectionGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext initContext)
        {
            // 获取所有包含 AotReflectionAttribute 的类型
            var classInfoProvider = initContext.SyntaxProvider.ForAttributeWithMetadataName(
                "Krins.Analyzer.Attributes.AotReflectionAttribute",
                (syntaxNode, _) => true,
                (syntaxContext, _) =>
                    (
                        syntaxContext.TargetSymbol,
                        syntaxContext.SemanticModel,
                        syntaxContext.TargetNode
                    )
            );

            // 为每个类生成代码
            initContext.RegisterSourceOutput(
                classInfoProvider,
                (sourceProductionContext, source) =>
                {
                    if (source.TargetSymbol is not INamedTypeSymbol classTypeSymbol)
                    {
                        return;
                    }

                    var sb = new StringBuilder();
                    // 添加 using 指令
                    var usingDirectives = source.TargetNode.GetUsingDirectives();
                    foreach (var usingDirective in usingDirectives)
                    {
                        sb.AppendLine(usingDirective);
                    }
                    sb.AppendLine();
                    sb.AppendLine($"namespace {classTypeSymbol.ContainingNamespace}");
                    sb.AppendLine("{");
                    sb.AppendLine(
                        $"    {classTypeSymbol.DeclaredAccessibility.GetAccessibilityLiteral()} partial class {classTypeSymbol.Name}"
                    );
                    sb.AppendLine("    {");
                    // Dictionary<string, Type> PropertyTypes
                    sb.AppendLine(
                        "        public static Dictionary<string, Type> PropertyTypes { get; } = new()"
                    );
                    sb.AppendLine("        {");
                    // 遍历所有属性
                    var propertySymbols = classTypeSymbol
                        .GetMembers()
                        .OfType<IPropertySymbol>()
                        .Where(p => p.DeclaredAccessibility == Accessibility.Public);
                    foreach (var propertySymbol in propertySymbols)
                    {
                        sb.AppendLine(
                            $"            {{nameof({propertySymbol.Name}), typeof({propertySymbol.Type.GetFullyQualifiedName()})}},"
                        );
                    }
                    sb.AppendLine("        };");
                    sb.AppendLine();
                    // object GetValue(string name)
                    sb.AppendLine("        public object GetValue(string name) => name switch");
                    sb.AppendLine("        {");
                    foreach (var propertySymbol in propertySymbols)
                    {
                        sb.AppendLine(
                            $"            nameof({propertySymbol.Name}) => {propertySymbol.Name},"
                        );
                    }
                    sb.AppendLine("            _ => throw new ArgumentException(nameof(name))");
                    sb.AppendLine("        };");
                    sb.AppendLine("    }");
                    sb.AppendLine("}");
                    sourceProductionContext.AddSource(
                        $"{classTypeSymbol.ContainingNamespace}.{classTypeSymbol.Name}.g.cs",
                        sb.ToString()
                    );
                }
            );
        }
    }
}
