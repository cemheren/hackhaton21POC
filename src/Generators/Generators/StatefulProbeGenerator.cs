//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Generators
{
    using GeneratorDependencies;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp.Syntax;
    using Microsoft.CodeAnalysis.Text;
    using System;
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
            //if (!Debugger.IsAttached)
            //{
            //    Debugger.Launch();
            //}
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

            var semanticModel = compilation.GetSemanticModel(userClass.SyntaxTree);
            var syntaxTreeRoot = userClass.SyntaxTree.GetRoot();

            var usingStatements = syntaxTreeRoot
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .ToArray();

            var methodBody = syntaxTreeRoot
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.ValueText == "ProbeImplementation")
                .Single()
                .Body;

            var segments = this.SplitOnInterleaverCalls(methodBody);

            var generatedMethodBody = $@"
                Console.WriteLine(""This is generated"");
                Console.WriteLine(""This is generated 2"");
";

            for (int i = 0; i < segments.Count; i++)
            {
                var stateSegment = segments[i];
                var nodesAsText = stateSegment.Select(node => node.GetText().ToString()).ToArray();
                var joinedSegment = string.Join("", nodesAsText);
                generatedMethodBody = $@" {generatedMethodBody}
                if (state == {i}) {{
                    {joinedSegment}
                    {(i != segments.Count - 1 ? $"state = {i + 1};" : "state = -1;")}
                    return;
                }}
                
";
            }

            var usingStatementsText = string.Join("", usingStatements.Select(statement => statement.GetText().ToString()).ToArray()); 

            // add the generated implementation to the compilation
            SourceText sourceText = SourceText.From($@"
namespace Hackathon21Poc.Probes {{
    {usingStatementsText}

    public partial class {userClass.Identifier}
    {{
        partial void GeneratedProbeImplementation(ref int state)
        {{
            {generatedMethodBody}
        }}
    }}
}}", Encoding.UTF8);
            context.AddSource("UserClass.Generated.cs", sourceText);
        }

        private List<List<StatementSyntax>> SplitOnInterleaverCalls(BlockSyntax methodBody)
        {
            var segments = new List<List<StatementSyntax>>();
            var interleaverIndexes = new List<int>();
            for (int i = 0; i < methodBody.Statements.Count; i++)
            {
                var statement = methodBody.Statements[i];

                if (statement is ExpressionStatementSyntax expressionStatement
                    && expressionStatement.Expression is InvocationExpressionSyntax invocationExpression
                    && invocationExpression.GetText().ToString().Contains("Interleaver.Pause"))
                {
                    if (interleaverIndexes.Count == 0)
                    {
                        segments.Add(methodBody.Statements.Take(i).ToList());
                    }
                    else
                    {
                        segments.Add(methodBody.Statements.Skip(interleaverIndexes.Last() + 1).Take(i - interleaverIndexes.Last() - 1).ToList());
                    }

                    interleaverIndexes.Add(i);
                }
            }

            segments.Add(methodBody.Statements.Skip(interleaverIndexes.Last() + 1).Take(methodBody.Statements.Count - interleaverIndexes.Last()).ToList());
            return segments;
        }

        class MySyntaxReceiver : ISyntaxReceiver
        {
            public ClassDeclarationSyntax ClassToAugment { get; private set; }

            public MethodDeclarationSyntax MethodToAugment { get; private set; }

            public void OnVisitSyntaxNode(SyntaxNode syntaxNode)
            {
                // Business logic to decide what we're interested in goes here
                if (syntaxNode is ClassDeclarationSyntax cds &&
                    cds.Identifier.ValueText == "UserClass")
                {
                    ClassToAugment = cds;
                }

                if (syntaxNode is MethodDeclarationSyntax method &&
                    method.Identifier.ValueText == "ProbeImplementation")
                {
                    MethodToAugment = method;
                }
            }
        }
    }
}
