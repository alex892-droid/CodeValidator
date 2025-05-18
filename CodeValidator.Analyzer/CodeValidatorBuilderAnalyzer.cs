using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Linq;
using System;
using System.Text.RegularExpressions;

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

            VerifyNullableProperties(context);
            VerifyClassesNames(context);
        }
        private void VerifyClassesNames(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol) ||
                methodSymbol.Name != "RequireClassNamePattern")
                return;

            string pattern = null, customMessage = "Class name does not match regex";

            var args = invocation.ArgumentList.Arguments;
            if (args.Count > 0) TryGetStringArgument(context, args[0], out pattern);
            if (args.Count > 1) TryGetStringArgument(context, args[1], out customMessage);

            if (!TryGetNamespaceFromChainedInvocation(context, invocation, out var ns, out var includeSubs))
                return;

            var regex = new Regex(pattern);
            var compilation = context.SemanticModel.Compilation;

            ForEachClassInNamespace(compilation, ns, includeSubs, (classSymbol, _) =>
            {
                if (!regex.IsMatch(classSymbol.Name))
                {
                    context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None, $"{customMessage}: class {classSymbol.Name}"));
                }
            });
        }

        private void VerifyNullableProperties(SyntaxNodeAnalysisContext context)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            if (!(context.SemanticModel.GetSymbolInfo(invocation).Symbol is IMethodSymbol methodSymbol) ||
                methodSymbol.Name != "RequireNullableProperties")
                return;

            string customMessage = "Property must be nullable";
            var args = invocation.ArgumentList.Arguments;
            if (args.Count > 0) TryGetStringArgument(context, args[0], out customMessage);

            if (!TryGetNamespaceFromChainedInvocation(context, invocation, out var ns, out var includeSubs))
                return;

            var compilation = context.SemanticModel.Compilation;

            ForEachClassInNamespace(compilation, ns, includeSubs, (classSymbol, _) =>
            {
                foreach (var prop in classSymbol.GetMembers().OfType<IPropertySymbol>())
                {
                    if (prop.NullableAnnotation != NullableAnnotation.Annotated)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(Rule, Location.None,
                            $"{customMessage}: property {prop.Name} in class {classSymbol.Name}."));
                    }
                }
            });
        }

        private bool TryGetStringArgument(SyntaxNodeAnalysisContext context, ArgumentSyntax arg, out string value)
        {
            value = null;
            var constValue = context.SemanticModel.GetConstantValue(arg.Expression);
            if (constValue.HasValue && constValue.Value is string str)
            {
                value = str;
                return true;
            }
            return false;
        }

        private bool TryGetNamespaceFromChainedInvocation(SyntaxNodeAnalysisContext context, InvocationExpressionSyntax invocation, out string ns, out bool includeSubNamespaces)
        {
            ns = null;
            includeSubNamespaces = false;

            if (invocation.Expression is MemberAccessExpressionSyntax memberAccess &&
                memberAccess.Expression is InvocationExpressionSyntax chainedInvocation &&
                chainedInvocation.Expression is MemberAccessExpressionSyntax chainedMemberAccess &&
                chainedMemberAccess.Expression is InvocationExpressionSyntax nsInvocation)
            {
                var nsSymbol = context.SemanticModel.GetSymbolInfo(nsInvocation).Symbol as IMethodSymbol;

                if (nsSymbol != null &&
                    nsInvocation.ArgumentList?.Arguments.Count > 0 &&
                    TryGetStringArgument(context, nsInvocation.ArgumentList.Arguments[0], out string nsStr))
                {
                    ns = nsStr;
                    includeSubNamespaces = nsSymbol.Name == "ForAllSubNamespacesOf";
                    return true;
                }
            }

            return false;
        }


        private void ForEachClassInNamespace(Compilation compilation, string ns, bool includeSubNamespaces, Action<INamedTypeSymbol, SemanticModel> action)
        {
            foreach (var tree in compilation.SyntaxTrees)
            {
                var model = compilation.GetSemanticModel(tree);
                var classes = tree.GetRoot().DescendantNodes().OfType<ClassDeclarationSyntax>();

                foreach (var classDecl in classes)
                {
                    var symbol = model.GetDeclaredSymbol(classDecl);
                    if (symbol != null)
                    {
                        var classNs = symbol.ContainingNamespace?.ToDisplayString();
                        if (classNs != null &&
                            (classNs == ns || (includeSubNamespaces && classNs.StartsWith(ns + "."))))
                        {
                            action(symbol, model);
                        }
                    }
                }
            }
        }
    }
}
