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

            var probeImplementationMethod = syntaxReceiver.MethodToAugment;
            var methodContents = this.GetMethodContents(probeImplementationMethod);
            var stateSegments = this.SplitOnInterleaverCalls(methodContents);

            //userClass.SyntaxTree.GetText().ToString().Substring(userClass.Members[1].ChildNodesAndTokens()[4].SpanStart, userClass.Members[1].ChildNodesAndTokens()[4].Span.Length)
            var probeImplementationMethodContents = userClass
                .Members[1]
                //.Where(member => true)
                .ChildNodesAndTokens()
                .Last();
            var methodNodes = probeImplementationMethodContents.ChildNodesAndTokens().Skip(1).Take(probeImplementationMethodContents.ChildNodesAndTokens().Count - 2);
            var methodContentsText = userClass.SyntaxTree.GetText().ToString().Substring(methodNodes.First().SpanStart, methodNodes.Last().SpanStart - methodNodes.First().SpanStart + methodNodes.Last().Span.Length);

            var semanticModel = compilation.GetSemanticModel(userClass.SyntaxTree);
            var methodBody = userClass.SyntaxTree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .Where(x => x.Identifier.ValueText == "ProbeImplementation")
                .Single()
                .Body;

            var generatedMethodBody = $@"
                Console.WriteLine(""This is generated"");
                Console.WriteLine(""This is generated 2"");
";

            for (int i = 0; i < stateSegments.Count; i++)
            {
                var stateSegment = stateSegments[i];
                var nodesAsText = stateSegment.Select(node => this.GetNodeText(node)).ToArray();
                var joinedSegment = string.Join("\n", nodesAsText);
                generatedMethodBody = $@"
                    {generatedMethodBody}
                    {joinedSegment}
";
            }

            // add the generated implementation to the compilation
            SourceText sourceText = SourceText.From($@"
namespace Hackathon21Poc.Probes {{
    using System;

    public partial class {userClass.Identifier}
    {{
        partial void GeneratedProbeImplementation()
        {{
            {generatedMethodBody}
        }}
    }}
}}", Encoding.UTF8);
            context.AddSource("UserClass.Generated.cs", sourceText);
        }

        private List<SyntaxNodeOrToken> GetMethodContents(MethodDeclarationSyntax method)
        {
            var methodBody = method.ChildNodes().Last();
            var nodeCount = methodBody.ChildNodesAndTokens().Count;
            return methodBody.ChildNodesAndTokens().Skip(1).Take(nodeCount - 2).ToList();
        }

        private List<List<SyntaxNodeOrToken>> SplitOnInterleaverCalls(List<SyntaxNodeOrToken> methodContents)
        {
            var stateSegments = new List<List<SyntaxNodeOrToken>>();
            var interleaverIndexes = new List<int>();
            for (int i = 0; i < methodContents.Count(); i++)
            {
                var node = methodContents[i];
                var nodeText = this.GetNodeText(node);
                if (nodeText == "Interleaver.Pause();")
                {
                    if (interleaverIndexes.Count == 0)
                    {
                        stateSegments.Add(methodContents.Take(i).ToList());
                    }
                    else
                    {
                        stateSegments.Add(methodContents.Skip(interleaverIndexes.Last() + 1).Take(i - interleaverIndexes.Last()).ToList());
                    }

                    interleaverIndexes.Add(i);
                }
            }

            stateSegments.Add(methodContents.Skip(interleaverIndexes.Last() + 1).Take(methodContents.Count - interleaverIndexes.Last()).ToList());
            return stateSegments;
        }

        private string GetNodeText(SyntaxNodeOrToken node)
        {
            return node.SyntaxTree.ToString().Substring(node.SpanStart, node.Span.Length);
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
