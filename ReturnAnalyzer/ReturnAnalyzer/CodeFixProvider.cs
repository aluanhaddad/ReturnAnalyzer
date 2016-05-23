using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Rename;
using Microsoft.CodeAnalysis.Text;
using static Microsoft.CodeAnalysis.CSharp.SyntaxFactory;

namespace ReturnAnalyzer
{
    [ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(ReturnAnalyzerCodeFixProvider)), Shared]
    public class ReturnAnalyzerCodeFixProvider : CodeFixProvider
    {
        private const string title = "Throw to indicate invalid path.";

        public sealed override ImmutableArray<string> FixableDiagnosticIds => ImmutableArray.Create(ReturnAnalyzerAnalyzer.DiagnosticId);

        public sealed override FixAllProvider GetFixAllProvider()
        {
            // See https://github.com/dotnet/roslyn/blob/master/docs/analyzers/FixAllProvider.md for more information on Fix All Providers
            return WellKnownFixAllProviders.BatchFixer;
        }

        public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
        {
            var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

            // TODO: Replace the following code with your own analysis, generating a CodeAction for each fix to suggest
            var diagnostic = context.Diagnostics.First();
            var diagnosticSpan = diagnostic.Location.SourceSpan;

            // Find the type return statement identified by the diagnostic.
            var enclosingMethodDeclaration = GetDeclaredTypeOfEnclosingMethod(root, diagnosticSpan);
            var returnStatement = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ReturnStatementSyntax>().FirstOrDefault();
            var lambda = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<LambdaExpressionSyntax>().FirstOrDefault();
            var arrow = root.FindToken(diagnosticSpan.Start).Parent.AncestorsAndSelf().OfType<ArrowExpressionClauseSyntax>().FirstOrDefault();
            // Register a code action that will invoke the fix.
            var valueProducingNode = returnStatement ?? lambda ?? arrow as CSharpSyntaxNode;

            context.RegisterCodeFix(
                CodeAction.Create(
                    title: title,
                    createChangedDocument: c => this.ThrowInvalidOperationExceptionAsync(context.Document, valueProducingNode, c),
                    equivalenceKey: title),
                diagnostic);

            var enclosingMethodGenericReturnType = enclosingMethodDeclaration as GenericNameSyntax;

            var enclosingMethodReturnType = enclosingMethodDeclaration;

            if ((enclosingMethodReturnType as PredefinedTypeSyntax)?.Keyword.IsKind(SyntaxKind.StringKeyword) == true)
            {
                context.RegisterCodeFix(
                   CodeAction.Create(
                       title: $"Return string.{nameof(string.Empty)}",
                       createChangedDocument: c => ReplaceWithEmptyStringAsync(context.Document, valueProducingNode, c),
                       equivalenceKey: $"Return string.{nameof(string.Empty)}"),
                   diagnostic);
            }
            else if (enclosingMethodGenericReturnType?.Identifier.Text == "IEnumerable")
            {
                context.RegisterCodeFix(
                    CodeAction.Create(
                        title: $"Return Empty{(enclosingMethodDeclaration.ChildTokens().First())}",
                        createChangedDocument: c => ReplaceWithEmtpyCollectionAsync(context.Document, valueProducingNode, CreateEnumerableDotEmptySyntax, c),
                        equivalenceKey: $"Return Empty{(enclosingMethodDeclaration.ChildTokens().First())}"),
                    diagnostic);
            }
            else
            {
                var returnType = (enclosingMethodGenericReturnType ?? enclosingMethodReturnType as TypeSyntax);

                var returnTypeName = returnType?.GetText()?.ToString();

                var genericType = Type.GetType(returnTypeName);
                if (returnType is ArrayTypeSyntax)
                {
                    context.RegisterCodeFix(
                        CodeAction.Create(
                            title: $"Use new {(returnType as ArrayTypeSyntax)?.ElementType}[0]",
                            createChangedDocument: c => ReplaceWithEmtpyCollectionAsync(context.Document, valueProducingNode, CreateEmptyArrayOfTypeCreationSyntax, c),
                            equivalenceKey: $"Return new {(returnType as ArrayTypeSyntax)?.ElementType}[]"),
                        diagnostic);
                }
                else
                {
                    var type = GetDeclaredTypeOfEnclosingMethod(root, diagnosticSpan);
                    if (type != null)
                    {
                        context.RegisterCodeFix(
                            CodeAction.Create(
                                title: $"Return Default({(type.ChildNodes().FirstOrDefault(p => p == enclosingMethodDeclaration.FirstAncestorOrSelf<IdentifierNameSyntax>(x => x.IsKind(SyntaxKind.TypeParameter))))})",
                                createChangedDocument: c => ReturnDefaultOfTResultAsync(context.Document, valueProducingNode, c),
                                equivalenceKey: $"Return Default({(enclosingMethodDeclaration)})"),
                            diagnostic);
                    }
                }
            }
        }

        private static async Task<Document> ReturnDefaultOfTResultAsync(Document document, CSharpSyntaxNode returningClause, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var enclosingMethodDeclarationOrLambdaTarget = GetDeclaredTypeOfEnclosingMethod(root, returningClause.Span);
            var enclosingMethodReturnType = enclosingMethodDeclarationOrLambdaTarget as GenericNameSyntax;
            var defaultExpressionSyntax = DefaultExpression(enclosingMethodDeclarationOrLambdaTarget);
            var defaultExpression = returningClause is ReturnStatementSyntax
                ? ReturnStatement(defaultExpressionSyntax)
                : returningClause is LambdaExpressionSyntax
                ? ParenthesizedLambdaExpression(defaultExpressionSyntax)
                : ArrowExpressionClause(defaultExpressionSyntax) as CSharpSyntaxNode;


            var newRoot = root.ReplaceNode(returningClause, defaultExpression);

            return document.WithSyntaxRoot(newRoot);
        }

        private async Task<Document> ReplaceWithEmptyStringAsync(Document document, CSharpSyntaxNode returningClause, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var @return = returningClause as ReturnStatementSyntax;
            CSharpSyntaxNode emtpyStringExpression = null;
            if (@return != null)
            {
                emtpyStringExpression = CreateReplacementReturn(@return, QualifiedName(IdentifierName("string"), IdentifierName(nameof(string.Empty))));
            }
            else
            {
                var lambda = returningClause as LambdaExpressionSyntax;
                if (lambda != null)
                {
                    emtpyStringExpression = CreateReplacementExpression(lambda, QualifiedName(IdentifierName("string"), IdentifierName(nameof(string.Empty))));
                }
                else
                {
                    var arrowExpresion = returningClause as ArrowExpressionClauseSyntax;
                    if (arrowExpresion != null)
                    {
                        emtpyStringExpression = CreateReplacementReturn(QualifiedName(IdentifierName("string"), IdentifierName(nameof(string.Empty))));
                    }

                }
            }
            var newRoot = root.ReplaceNode(returningClause, emtpyStringExpression);

            return document.WithSyntaxRoot(newRoot);
        }

        private LambdaExpressionSyntax CreateReplacementExpression(LambdaExpressionSyntax lambda, ExpressionSyntax replacementExpression) =>
            (lambda is SimpleLambdaExpressionSyntax
                ? SimpleLambdaExpression((lambda as SimpleLambdaExpressionSyntax)?.Parameter, replacementExpression).WithAsyncKeyword(lambda.AsyncKeyword)
                : ParenthesizedLambdaExpression(replacementExpression).WithAsyncKeyword(lambda.AsyncKeyword) as LambdaExpressionSyntax).WithTriviaFrom(lambda);

        private ArrowExpressionClauseSyntax CreateReplacementReturn(ExpressionSyntax replacementExpression) => ArrowExpressionClause(replacementExpression);

        private static ReturnStatementSyntax CreateReplacementReturn(ReturnStatementSyntax returnStatement, ExpressionSyntax replacementExpression)
        {
            return ReturnStatement(returnStatement.ReturnKeyword, replacementExpression, returnStatement.SemicolonToken)
                .WithTrailingTrivia(returnStatement.Parent.GetTrailingTrivia());
        }

        private async Task<Document> ThrowInvalidOperationExceptionAsync(Document document, CSharpSyntaxNode returningClause, CancellationToken cancellationToken)
        {

            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var returnStatement = returningClause as ReturnStatementSyntax;
            SyntaxNode newRoot;
            if (returnStatement != null)
            {
                var throwStatement = ThrowStatement()
                    .WithExpression(ObjectCreationExpression(IdentifierName(nameof(InvalidOperationException)))
                    .WithArgumentList(ArgumentList())).WithLeadingTrivia(returnStatement.ReturnKeyword.GetAllTrivia()).WithTrailingTrivia(returnStatement.SemicolonToken.GetAllTrivia());
                newRoot = root.ReplaceNode(returningClause, throwStatement);
            }
            else
            {
                var simpleLambda = returningClause as SimpleLambdaExpressionSyntax;
                if (simpleLambda != null)
                {
                    newRoot = root.ReplaceNode(simpleLambda, simpleLambda.WithArrowToken(simpleLambda.ArrowToken).WithBody(Block(ThrowStatement()
                        .WithExpression(ObjectCreationExpression(IdentifierName(nameof(InvalidOperationException))).WithArgumentList(ArgumentList())))));
                }
                else
                {
                    var parenthesizedLambda = returningClause as ParenthesizedLambdaExpressionSyntax;
                    if (parenthesizedLambda != null)
                    {
                        newRoot = root.ReplaceNode(parenthesizedLambda, parenthesizedLambda.WithArrowToken(parenthesizedLambda.ArrowToken).WithBody(Block(ThrowStatement()
                            .WithExpression(ObjectCreationExpression(IdentifierName(nameof(InvalidOperationException))).WithArgumentList(ArgumentList())))));
                    }
                    else
                    {
                        var arrow = returningClause as ArrowExpressionClauseSyntax;
                        var methodOrProperty = returningClause.FirstAncestorOrSelf((MemberDeclarationSyntax md) => md.IsKind(SyntaxKind.PropertyDeclaration) || md.IsKind(SyntaxKind.MethodDeclaration));
                        var expressionProperty = methodOrProperty as PropertyDeclarationSyntax;
                        if (expressionProperty != null)
                        {
                            var propertyDeclarationSyntax = ExpressionPropertyToGetterWithThrowStatement(expressionProperty);
                            newRoot = root.ReplaceNode(expressionProperty, propertyDeclarationSyntax);
                        }
                        else
                        {
                            var expressionMethod = methodOrProperty as MethodDeclarationSyntax;
                            if (expressionMethod != null)
                            {
                                var methodDeclarationSyntax = ExpressionMethodToThrowStatementBody(expressionMethod);
                                newRoot = root.ReplaceNode(methodOrProperty, methodDeclarationSyntax);
                            }
                            else return document;
                        }
                    }
                }
            }
            return document.WithSyntaxRoot(newRoot);
        }

        private MethodDeclarationSyntax ExpressionMethodToThrowStatementBody(MethodDeclarationSyntax method) =>
            MethodDeclaration(method.ReturnType, method.Identifier)
                .WithModifiers(method.Modifiers)
                .WithTypeParameterList(method.TypeParameterList)
                .WithAttributeLists(method.AttributeLists)
                .WithConstraintClauses(method.ConstraintClauses)
                .WithExplicitInterfaceSpecifier(method.ExplicitInterfaceSpecifier)
                .WithTriviaFrom(method)
                .WithBody(Block(ThrowStatement().WithExpression(ObjectCreationExpression(IdentifierName(nameof(InvalidOperationException))).WithArgumentList(ArgumentList()))));

        private PropertyDeclarationSyntax ExpressionPropertyToGetterWithThrowStatement(PropertyDeclarationSyntax property) =>
            PropertyDeclaration(property.Type, property.Identifier)
                .WithModifiers(property.Modifiers)
                .WithAttributeLists(property.AttributeLists)
                .WithTriviaFrom(property)
                .WithAccessorList(AccessorList(SingletonList(AccessorDeclaration(SyntaxKind.GetAccessorDeclaration, Block(ThrowStatement()
                    .WithExpression(ObjectCreationExpression(IdentifierName(nameof(InvalidOperationException))).WithArgumentList(ArgumentList())))))));

        private async Task<Document> ReplaceWithEmtpyCollectionAsync(Document document, CSharpSyntaxNode returningClause, Func<SeparatedSyntaxList<TypeSyntax>, ExpressionSyntax> expressionFactory, CancellationToken cancellationToken)
        {
            var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
            var enclosingMethodType = GetDeclaredTypeOfEnclosingMethod(root, returningClause.Span);
            var genericReturnTypeArgumwnt = enclosingMethodType as GenericNameSyntax;
            var typeArgs = genericReturnTypeArgumwnt?.TypeArgumentList?.Arguments ?? SingletonSeparatedList((enclosingMethodType as ArrayTypeSyntax)?.ElementType);

            ExpressionSyntax newValueExpression = expressionFactory(typeArgs);


            SyntaxNode newRoot;
            var returnStatement = returningClause as ReturnStatementSyntax;
            if (returnStatement != null)
            {
                var emptyEnumerableExpression = WithEmptyCollectionOfTypeExpressionSyntax(ReturnStatement()
                                .WithReturnKeyword(returnStatement.ReturnKeyword), newValueExpression);
                newRoot = root.ReplaceNode(returningClause, emptyEnumerableExpression);
            }
            else
            {
                var lambda = returningClause as LambdaExpressionSyntax;


                if (lambda != null)
                {
                    var emptyEnumerableExpression = WithEmptyCollectionOfTypeExpressionSyntax(lambda, newValueExpression);
                    newRoot = root.ReplaceNode(returningClause, emptyEnumerableExpression);
                }
                else
                {
                    var arrow = returningClause as ArrowExpressionClauseSyntax;
                    var emptyEnumerableExpression = WithEmptyCollectionOfTypeExpressionSyntax(arrow, newValueExpression);
                    newRoot = root.ReplaceNode(returningClause, emptyEnumerableExpression);
                }
            }
            return document.WithSyntaxRoot(newRoot);
        }

        private static CSharpSyntaxNode WithEmptyCollectionOfTypeExpressionSyntax(CSharpSyntaxNode returningClause, ExpressionSyntax expression)
        {
            var returnStatementSyntax = returningClause as ReturnStatementSyntax;
            if (returnStatementSyntax != null)
            {
                return returnStatementSyntax.WithExpression(expression);
            }

            var lambdaExpressionSyntax = returningClause as LambdaExpressionSyntax;
            if (lambdaExpressionSyntax != null)
            {
                return lambdaExpressionSyntax is SimpleLambdaExpressionSyntax
                ? SimpleLambdaExpression((lambdaExpressionSyntax as SimpleLambdaExpressionSyntax).Parameter, expression)
                : ParenthesizedLambdaExpression(expression) as LambdaExpressionSyntax;
            }

            if (returningClause is ArrowExpressionClauseSyntax)
            {
                return ArrowExpressionClause(expression);
            }

            throw new InvalidOperationException("Unknown value clause");

        }

        private static InvocationExpressionSyntax CreateEnumerableDotEmptySyntax(SeparatedSyntaxList<TypeSyntax> typeArgs) => InvocationExpression(
            GenericName(
                Identifier(
                    QualifiedName(
                        IdentifierName(
                            nameof(Enumerable)
                        ),
                        IdentifierName(
                            nameof(Enumerable.Empty)
                        )
                    )
                    .ToFullString()
                ),
            TypeArgumentList(typeArgs))
        );

        private static TypeSyntax GetDeclaredTypeOfEnclosingMethod(SyntaxNode root, TextSpan diagnosticSpan) => root.FindToken(diagnosticSpan.Start)
            .Parent?.Ancestors()
            .Select(x => (x as CSharpSyntaxNode).Accept(new InvocableMemberVisitor()))
            .FirstOrDefault(x => x != null);

        private static ArrayCreationExpressionSyntax CreateEmptyArrayOfTypeCreationSyntax(SeparatedSyntaxList<TypeSyntax> typeArguments)
        {
            return ArrayCreationExpression(ArrayType(typeArguments[0], SingletonList(ArrayRankSpecifier(SingletonSeparatedList<ExpressionSyntax>(LiteralExpression(SyntaxKind.NumericLiteralExpression, Literal(0)))))));
        }
    }

}