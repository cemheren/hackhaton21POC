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

            var generatedMethodBody = "";

            var semanticModel = compilation.GetSemanticModel(userClass.SyntaxTree);
            var syntaxTreeRoot = userClass.SyntaxTree.GetRoot();

            var usingStatements = syntaxTreeRoot
                .DescendantNodes()
                .OfType<UsingDirectiveSyntax>()
                .ToArray();

            var usingStatementsText = string.Join("", usingStatements.Select(statement => statement.GetText().ToString()).ToArray());

            var methodBody = syntaxTreeRoot
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.ValueText == "ProbeImplementation")
                .Single()
                .Body;

            var statementsSplitByInterleaver = new List<List<StatementSyntax>>();
            statementsSplitByInterleaver.Add(new List<StatementSyntax>()); // add the initial one.

            var i = 0;
            var interleaverCount = 0;
            while (i < methodBody.Statements.Count)
            {
                for (; i < methodBody.Statements.Count; i++)
                {
                    var statement = methodBody.Statements[i];

                    if (statement is ExpressionStatementSyntax expressionStatement
                        && expressionStatement.Expression is InvocationExpressionSyntax invocationExpression
                        && invocationExpression.GetText().ToString().Contains("Interleaver.Pause"))
                    {
                        statementsSplitByInterleaver.Add(new List<StatementSyntax>()); // add the initial one.
                        interleaverCount++;
                        i++;
                        break;
                    }
            
                    statementsSplitByInterleaver[interleaverCount].Add(statement);
                }
            }

            for (i = 0; i < statementsSplitByInterleaver.Count; i++)
            {
                var stateSegment = statementsSplitByInterleaver[i];
                var nodesAsText = stateSegment.Select(node => node.GetText().ToString()).ToArray();
                var joinedSegment = string.Join("", nodesAsText);
                generatedMethodBody = $@" {generatedMethodBody}
                if (state.ExecutionState == {i}) {{
                    {joinedSegment}
                    {(i != statementsSplitByInterleaver.Count - 1 ? $"state.ExecutionState = {i + 1};" : "state.ExecutionState = -1;")}
                    return;
                }}
                
";
            }

            var variables = new List<VariableDeclarationSyntax>();
            foreach (var statementGroup in statementsSplitByInterleaver)
            foreach (var statement in statementGroup)
            {
                if (statement is LocalDeclarationStatementSyntax localDeclaration
                        && localDeclaration.Declaration is VariableDeclarationSyntax variableDeclaration)
                {
                    variables.Add(variableDeclaration);
                }
            }


            // add the generated implementation to the compilation
            SourceText sourceText = SourceText.From($@"
namespace Hackathon21Poc.Probes {{
    {usingStatementsText}

    public partial class {userClass.Identifier}State
    {{
        {GetProperties(semanticModel, variables)}
    }}

    public partial class {userClass.Identifier}
    {{
        public partial void GeneratedProbeImplementation<T>(T state) where T : InterleaverState
        {{
            {
                generatedMethodBody
            }
            System.Diagnostics.Debug.WriteLine(""test"");
        }}
    }}
}}", Encoding.UTF8);
            context.AddSource("UserClass.Generated.cs", sourceText);
        }

        private string GetLines(List<List<StatementSyntax>> statementGroups)
        { 
            var sb = new StringBuilder();
            foreach (var statementGroup in statementGroups)
            foreach (var statement in statementGroup)
            { 
                sb.AppendLine(statement.ToString());
                sb.Append("            "); // Indent
            }

            return sb.ToString();
        }

        private string GetProperties(SemanticModel sm, List<VariableDeclarationSyntax> variables)
        {
            var sb = new StringBuilder();
            foreach (var variable in variables)
            {
                var type = variable.Type.ToString();
                var declarator = variable.ChildNodes().OfType<VariableDeclaratorSyntax>().First();

                sb.AppendLine($"public {type} {declarator.Identifier.ValueText};");
                sb.Append("        "); // Indent
            }

            return sb.ToString();
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
