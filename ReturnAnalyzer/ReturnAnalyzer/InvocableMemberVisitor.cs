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
    class InvocableMemberVisitor : CSharpSyntaxVisitor<TypeSyntax>
    {
        public override TypeSyntax VisitMethodDeclaration(MethodDeclarationSyntax node) => node?.ReturnType;

        public override TypeSyntax VisitAnonymousMethodExpression(AnonymousMethodExpressionSyntax node) => GetInferredReturnTypeOfAnonymousFunctionSyntax(node);

        public override TypeSyntax VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node) => GetInferredReturnTypeOfAnonymousFunctionSyntax(node);

        private static TypeSyntax GetInferredReturnTypeOfAnonymousFunctionSyntax(AnonymousFunctionExpressionSyntax node)
        {
            var target = node
                ?.FirstAncestorOrSelf<VariableDeclarationSyntax>()?.Type;
            var delegateVariableReturnType = (target as GenericNameSyntax)?.TypeArgumentList?.Arguments.LastOrDefault();
            if (delegateVariableReturnType != null) return delegateVariableReturnType;
            return node
                ?.FirstAncestorOrSelf<InvocationExpressionSyntax>()
                ?.ArgumentList.FirstAncestorOrSelf<GenericNameSyntax>()?.TypeArgumentList?.Arguments.LastOrDefault();

        }
        
        public override TypeSyntax VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node) => GetInferredReturnTypeOfAnonymousFunctionSyntax(node);

        public override TypeSyntax VisitPropertyDeclaration(PropertyDeclarationSyntax node) => node?.Type;
    }
}
