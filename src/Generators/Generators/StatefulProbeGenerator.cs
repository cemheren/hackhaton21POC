//-----------------------------------------------------------
// Copyright (c) Microsoft Corporation.  All rights reserved.
//-----------------------------------------------------------

namespace Hackathon21Poc.Generators
{
    using GeneratorDependencies;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
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
                        && expressionStatement.Expression is InvocationExpressionSyntax invocationExpression)
                    {
                        if (invocationExpression.GetText().ToString().Contains("Interleaver.Pause"))
                        {
                            statementsSplitByInterleaver.Add(new List<StatementSyntax>()); // add the initial one.
                            interleaverCount++; i++;
                            break;
                        }
                        
                        if (invocationExpression.GetText().ToString().Contains("Interleaver.Wait"))
                        {
                            statementsSplitByInterleaver.Add(new List<StatementSyntax>());
                            interleaverCount++; i++;

                            var waitTime = invocationExpression.ArgumentList.Arguments[0].Expression.GetText().ToString();

                            var waitString = $@"{{TimeSpan elapsedTime = DateTime.UtcNow - state.CurrentStateStartTime;

                            if (elapsedTime < {waitTime}) {{
                                Console.WriteLine($""elapsed time: {{elapsedTime}}"");
                                return;  
                            }}

                            Console.WriteLine($""Finished waiting {{{waitTime}}}"");
                            }}";

                            var block = SyntaxFactory.ParseStatement(waitString);

                            statementsSplitByInterleaver[interleaverCount].Add(block);

                            statementsSplitByInterleaver.Add(new List<StatementSyntax>()); // add the initial one.
                            interleaverCount++;
                            break;
                        }
                    }
            
                    statementsSplitByInterleaver[interleaverCount].Add(statement);
                }
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

            for (i = 0; i < statementsSplitByInterleaver.Count; i++)
            {
                var stateSegment = statementsSplitByInterleaver[i];
                var stateSegmentLines = GetLines(stateSegment, variables);

                generatedMethodBody = $@" {generatedMethodBody}
                if (state.ExecutionState == {i}) {{
                    {stateSegmentLines}
                    {(i != statementsSplitByInterleaver.Count - 1 ? $"state.ExecutionState = {i + 1};" : "state.ExecutionState = -1;")}
                    state.CurrentStateStartTime = DateTime.UtcNow;
                    return;
                }}
                
";
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
        public partial void GeneratedProbeImplementation({userClass.Identifier}State state)
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

        private string GetLines(List<StatementSyntax> statementGroup, List<VariableDeclarationSyntax> variables)
        { 
            var sb = new StringBuilder();
            //foreach (var statementGroup in statementGroups)
            foreach (var statement in statementGroup)
            {
                // Make sure to capture state variable's initial values.
                if (statement is LocalDeclarationStatementSyntax localDeclaration
                        && localDeclaration.Declaration is VariableDeclarationSyntax variableDeclaration)
                {
                    var declarator = variableDeclaration.ChildNodes().OfType<VariableDeclaratorSyntax>().First();
                    sb.AppendLine($"state.{declarator.Identifier.ValueText} = {declarator.Initializer.Value};");
                    continue;
                }

                if (statement is ExpressionStatementSyntax expressionStatement)
                {
                    if (expressionStatement.Expression is AssignmentExpressionSyntax assignmentExpression
                            && assignmentExpression.Left is IdentifierNameSyntax identifierNameSyntax)
                    {
                        // note: there are better ways to replace syntax elements in the expression. but this is ok for POC
                        var statefulStatement =
                            statement.ToString().Replace($"{identifierNameSyntax.Identifier} =", $"state.{identifierNameSyntax.Identifier} =");

                        sb.AppendLine(statefulStatement);
                    }
                    else if (expressionStatement.Expression is InvocationExpressionSyntax invocationExpression
                                && invocationExpression.ArgumentList is ArgumentListSyntax argumentListSyntax)
                    {
                        // Rudimentary way to rewrite functions that have variables. This probably needs to recurse and evaluate every syntax element
                        // so that we don't miss random things like:
                        // Console.Write($"{x}");
                        var invocationExpressionBuilder = new StringBuilder();
                        invocationExpressionBuilder.Append(invocationExpression.Expression.ToString());
                        invocationExpressionBuilder.Append("(");

                        foreach (var argument in argumentListSyntax.Arguments)
                        {
                            if (argument.Expression is IdentifierNameSyntax methodidentifierNameSyntax)
                            {
                                // todo: this can be hashed 
                                var replacableParam = variables.FirstOrDefault(variable => variable
                                    .ChildNodes()
                                    .OfType<VariableDeclaratorSyntax>()
                                    .First()
                                    .Identifier
                                    .Text == methodidentifierNameSyntax.Identifier.Text);

                                if (replacableParam != null)
                                {
                                    invocationExpressionBuilder.Append($"state.{methodidentifierNameSyntax.Identifier.Text}");
                                    continue;
                                }
                            }

                            invocationExpressionBuilder.Append(argument.Expression.ToString());
                        }
                        invocationExpressionBuilder.Append(");");
                        sb.AppendLine(invocationExpressionBuilder.ToString());
                    }
                }
                else
                {
                    sb.AppendLine(statement.ToString());
                }

                sb.Append("            "); // Indent the next line
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
