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
        [TestMethod]
        public void SimpleGeneratorTest()
        {
            string userSource = @"
namespace MyCode
{
    public partial class UserClass
    {
        protected void ProbeImplementation()
        {
            var x = 5;
            var y = 5;

            Interleaver.Pause();
        }

        public void RunAsync()
        {
            this.GeneratedProbeImplementation();
        }

        partial void GeneratedProbeImplementation();
    }
}
";
            Compilation comp = CreateCompilation(userSource);
            var newComp = RunGenerators(comp, out var generatorDiags, new StatefulProbeGenerator());

            Assert.IsNull(generatorDiags);
            Assert.IsNull(newComp.GetDiagnostics());
        }

        private static Compilation CreateCompilation(string source)
            => CSharpCompilation.Create("compilation",
                new[] { CSharpSyntaxTree.ParseText(source, new CSharpParseOptions(LanguageVersion.Preview)) },
                new[] { MetadataReference.CreateFromFile(typeof(Binder).GetTypeInfo().Assembly.Location) },
                new CSharpCompilationOptions(OutputKind.ConsoleApplication));
        
        private static Compilation RunGenerators(Compilation c, out ImmutableArray<Diagnostic> diagnostics, params ISourceGenerator[] generators)
        {
            CSharpGeneratorDriver.Create(generators).RunGeneratorsAndUpdateCompilation(c, out var d, out diagnostics);
            return d;
        }
    }
 }

