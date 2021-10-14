using System.Collections.Immutable;
using System.Linq;
using System.Reflection;
using Hackathon21Poc.Generators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace GeneratorUnitTest
{
    [TestClass]
    public class UnitTest1
    {
        private static string Filler =>
@"
    public class Interleaver
    {
        public static void Pause() { }
    }

    public class InterleaverState
    {
        public int ExecutionState;
    }

    public class Program
    {
        public static void Main(string[] args)
        {
        }
    }
"
;
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            string userSource = @$"
namespace Hackathon21Poc.Probes
{{
    using System;

    {Filler}

    public partial class UserClassState : InterleaverState
    {{ }}

    public partial class UserClass
    {{
        protected void ProbeImplementation()
        {{
            int x = 5;
            int y = 5;

            Interleaver.Pause();
        }}

        public void RunAsync()
        {{
            var st = new UserClassState();
            this.GeneratedProbeImplementation(st);
        }}

        public partial void GeneratedProbeImplementation<T>(T state) where T : InterleaverState;
    }}
}}
";
            Compilation comp = CreateCompilation(userSource);
            var errors = comp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            var newComp = RunGenerators(comp, out var generatorDiags, new StatefulProbeGenerator());

            Assert.AreEqual(0, generatorDiags.Length);
            errors = newComp.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error).ToList();

            Assert.AreEqual(0, errors.Count, 
                message: string.Join("\n ", errors));
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions()) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.WindowsApplication));
        
        private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CSharpGeneratorDriver.Create(generators).RunGeneratorsAndUpdateCompilation(c, out var outputCompilation, out diagnostics);
            return outputCompilation;
        }
    }
 }

