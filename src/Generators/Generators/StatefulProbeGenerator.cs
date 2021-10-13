//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Generators
{
    using GeneratorDependencies;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Text;

    [Generator]
    public class StatefulProbeGenerator : ISourceGenerator
    {
        public void Initialize(GeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif 

            // Register a factory that can create our custom syntax receiver
            context.RegisterForSyntaxNotifications(() => new MySyntaxReceiver());
        }

        public void Execute(GeneratorExecutionContext context)
        {
            var compilation = context.Compilation;

            // the generator infrastructure will create a receiver and populate it
            // we can retrieve the populated instance via the context
            MySyntaxReceiver syntaxReceiver = (MySyntaxReceiver)context.SyntaxReceiver;

            // get the recorded user class
            ClassDeclarationSyntax userClass = syntaxReceiver.ClassToAugment;
            if (userClass is null)
            {
                // if we didn't find the user class, there is nothing to do
                return;
            }
            //userClass.SyntaxTree.GetText().ToString().Substring(userClass.Members[1].ChildNodesAndTokens()[4].SpanStart, userClass.Members[1].ChildNodesAndTokens()[4].Span.Length)
            var probeImplementationMethodContents = userClass.Members[1].ChildNodesAndTokens().Last();
            var methodNodes = probeImplementationMethodContents.ChildNodesAndTokens().Skip(1).Take(probeImplementationMethodContents.ChildNodesAndTokens().Count - 2);
            var methodContents = userClass.SyntaxTree.GetText().ToString().Substring(methodNodes.First().SpanStart, methodNodes.Last().SpanStart - methodNodes.First().SpanStart + methodNodes.Last().Span.Length);

            var semanticModel = compilation.GetSemanticModel(userClass.SyntaxTree);
            var methodBody = userClass.SyntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.ValueText == "ProbeImplementation")
                .Single()
                .Body;

            var statementsBeforeInterleaver = new List<StatementSyntax>();

            for (int i = 0; i < methodBody.Statements.Count; i++)
            {
                var statement = methodBody.Statements[i];

                if (statement is ExpressionStatementSyntax expressionStatement
                    && expressionStatement.Expression is InvocationExpressionSyntax invocationExpression
                    && invocationExpression.GetText().ToString().Contains("Interleaver.Pause"))
                {
                    break;
                }

                statementsBeforeInterleaver.Add(statement);
            }

            // add the generated implementation to the compilation
            SourceText sourceText = SourceText.From($@"
namespace Hackathon21Poc.Probes {{
    using System;

    public partial class {userClass.Identifier}
    {{
        partial void GeneratedProbeImplementation()
        {{
            {GetLines(statementsBeforeInterleaver)}
            Console.WriteLine(""This is generated"");
        }}
    }}
}}", Encoding.UTF8);
            context.AddSource("UserClass.Generated.cs", sourceText);
        }

        private string GetLines(List<StatementSyntax> statements)
        { 
            var sb = new StringBuilder();
            foreach (var statement in statements)
            { 
                sb.AppendLine(statement.ToString());
            }

            return sb.ToString();
        }

        class MySyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax ClassToAugment { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Business logic to decide what we're interested in goes here
                if (syntaxNode is ClassDeclarationSyntax cds &&
                    cds.Identifier.ValueText == "UserClass")
                {
                    ClassToAugment = cds;
                }
            }
        }
    }
}
