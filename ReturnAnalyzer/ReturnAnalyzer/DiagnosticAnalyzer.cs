using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace ReturnAnalyzer
{
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public class ReturnAnalyzerAnalyzer : DiagnosticAnalyzer
    {
        public const string DiagnosticId = "ReturnAnalyzer";

        // You can change these strings in the Resources.resx file. If you do not want your analyzer to be localize-able, you can use regular strings for Title and MessageFormat.
        // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Localizing%20Analyzers.md for more on localization
        private static readonly LocalizableString Title = new LocalizableResourceString(nameof(Resources.AnalyzerTitle), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString MessageFormat = new LocalizableResourceString(nameof(Resources.AnalyzerMessageFormat), Resources.ResourceManager, typeof(Resources));
        private static readonly LocalizableString Description = new LocalizableResourceString(nameof(Resources.AnalyzerDescription), Resources.ResourceManager, typeof(Resources));
        private const string Category = "Return";

        private static DiagnosticDescriptor Rule = new DiagnosticDescriptor(DiagnosticId, Title, MessageFormat, Category, DiagnosticSeverity.Warning, isEnabledByDefault: true, description: Description);

        public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => ImmutableArray.Create(Rule);

        public override void Initialize(AnalysisContext context)
        {
            // TODO: Consider registering other actions that act on syntax instead of or in addition to symbols
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/Analyzer%20Actions%20Semantics.md for more information
            context.RegisterSyntaxNodeAction(AnalyzeSyntax, SyntaxKind.ReturnStatement, SyntaxKind.SimpleLambdaExpression, SyntaxKind.ArrowExpressionClause, SyntaxKind.ParenthesizedLambdaExpression);
        }

        private static void AnalyzeSyntax(SyntaxNodeAnalysisContext context)
        {
            // TODO: Replace the following code with your own analysis, generating Diagnostic objects for any issues you find
            var returningToken = context.Node as ReturnStatementSyntax ?? context.Node as LambdaExpressionSyntax ?? context.Node as ArrowExpressionClauseSyntax as CSharpSyntaxNode;
            var targetExpresion = (context.Node as ReturnStatementSyntax)?.Expression ?? (context.Node as LambdaExpressionSyntax)?.Body ?? (context.Node as ArrowExpressionClauseSyntax )?.Expression;

            if (targetExpresion.IsKind(SyntaxKind.NullLiteralExpression) &&
                !returningToken?.FirstAncestorOrSelf<CSharpSyntaxNode>(x => x.Accept(new InvocableMemberVisitor()) != null)?
                    .IsKind(SyntaxKind.NullableType) == true)
            {
                // For all such symbols, produce a diagnostic.
                var diagnostic = Diagnostic.Create(Rule, targetExpresion.GetLocation(), "null");

                context.ReportDiagnostic(diagnostic);
            }
        }

        private static TypeSyntax GetEnclosingMethodDeclaration(SyntaxNode root, TextSpan diagnosticSpan)
        {
            return root.FindToken(diagnosticSpan.Start).Parent?.Ancestors().Select(x => (x as CSharpSyntaxNode).Accept(new InvocableMemberVisitor())).FirstOrDefault(x => x != null);

        }


    }
}
