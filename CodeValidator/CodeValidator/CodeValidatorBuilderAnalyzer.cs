using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;

namespace CodeValidator
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class CodeValidatorBuilderAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "CVA";

        private static readonly DiagnosticDescriptor Rule = new DiagnosticDescriptor(
            DiagnosticId,
            "CodeValidatorBuilder error",
            "{0}", // Pure custom message passthrough
            "Usage",
            DiagnosticSeverity.Error,
            isEnabledByDefault: true);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
            context.EnableConcurrentExecution();
            context.RegisterSyntaxNodeAction(AnalyzeInvocation, SyntaxKind.InvocationExpression);
        }

        private void AnalyzeInvocation(SyntaxNodeAnalysisContext context)
        {
            //System.Diagnostics.Debugger.Launch();

            var invocation = (InvocationExpressionSyntax)context.Node;
            var symbolInfo = context.SemanticModel.GetSymbolInfo(invocation);
            var methodSymbol = symbolInfo.Symbol as IMethodSymbol;

            if (methodSymbol == null)
                return;

            if (methodSymbol.Name == "MustBeNullable")
            {
                string customMessage = "Property must be nullable";
                string targetNamespace = null;

                // Extract message from first argument
                if (invocation.ArgumentList?.Arguments.Count > 0)
                {
                    var firstArg = invocation.ArgumentList.Arguments[0].Expression;
                    var constantValue = context.SemanticModel.GetConstantValue(firstArg);

                    if (constantValue.HasValue && constantValue.Value is string message)
                    {
                        customMessage = message;
                    }
                }

                // Walk up to find ForNamespace call (via chained calls)
                if (invocation.Expression is MemberAccessExpressionSyntax memberAccess)
                {
                    if (memberAccess.Expression is InvocationExpressionSyntax chainedInvocation)
                    {
                        var chainedSymbol = context.SemanticModel.GetSymbolInfo(chainedInvocation).Symbol as IMethodSymbol;
                        if (chainedSymbol != null && chainedSymbol.Name == "ForAllProperties")
                        {
                            // Go up again to get ForNamespace
                            if (chainedInvocation.Expression is MemberAccessExpressionSyntax chainedMemberAccess)
                            {
                                if (chainedMemberAccess.Expression is InvocationExpressionSyntax nsInvocation)
                                {
                                    var nsSymbol = context.SemanticModel.GetSymbolInfo(nsInvocation).Symbol as IMethodSymbol;
                                    if (nsSymbol != null && nsSymbol.Name == "ForNamespace")
                                    {
                                        // Extract namespace from first argument
                                        if (nsInvocation.ArgumentList?.Arguments.Count > 0)
                                        {
                                            var nsArg = nsInvocation.ArgumentList.Arguments[0].Expression;
                                            var nsValue = context.SemanticModel.GetConstantValue(nsArg);
                                            if (nsValue.HasValue && nsValue.Value is string nsStr)
                                            {
                                                targetNamespace = nsStr;
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }

                if (targetNamespace != null)
                {
                    // Now inspect the compilation for properties in the namespace
                    var compilation = context.SemanticModel.Compilation;
                    foreach (var tree in compilation.SyntaxTrees)
                    {
                        var semanticModel = compilation.GetSemanticModel(tree);
                        var root = tree.GetRoot();

                        var classDeclarations = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
                        foreach (var classDecl in classDeclarations)
                        {
                            var classSymbol = semanticModel.GetDeclaredSymbol(classDecl);

                            if (classSymbol == null)
                                continue;

                            if (classSymbol.ContainingNamespace?.ToDisplayString() == targetNamespace)
                            {
                                foreach (var property in classSymbol.GetMembers().OfType<IPropertySymbol>())
                                {
                                    if (property.NullableAnnotation != NullableAnnotation.Annotated)
                                    {
                                        var diagnostic = Diagnostic.Create(
                                            Rule,
                                            Location.None,
                                            $"{customMessage}: property {property.Name} in class {classSymbol.Name}.");

                                        context.ReportDiagnostic(diagnostic);
                                    }
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
