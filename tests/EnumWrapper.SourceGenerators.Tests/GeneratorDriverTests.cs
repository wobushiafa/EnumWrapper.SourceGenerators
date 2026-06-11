using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using EnumWrapper.SourceGenerators;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestNamespace
{
    [TestClass]
    public class GeneratorDriverTests
    {
        [TestMethod]
        public void GeneratorDriver_CopiesXmlDocs_AndUsesUnsignedJsonParsing()
        {
            const string source = """
                using System.ComponentModel;
                using EnumWrapper.SourceGenerators;

                namespace Sample;

                /// <summary>
                /// Payment state summary.
                /// </summary>
                [GenerateEnumWrapper]
                public enum PaymentState : ulong
                {
                    /// <summary>
                    /// Pending summary.
                    /// </summary>
                    [Description("Pending payment")]
                    Pending = 0,

                    Paid = 18446744073709551615UL
                }
                """;

            var result = RunGenerator(source);
            AssertNoErrors(result);

            var generated = result.GetGeneratedSource("Sample.PaymentStateWrapper.g.cs");
            StringAssert.Contains(generated, "Payment state summary.");
            StringAssert.Contains(generated, "Pending summary.");
            StringAssert.Contains(generated, "reader.TryGetUInt64");
        }

        [TestMethod]
        public void GeneratorDriver_GeneratesWpfConverter_WhenIValueConverterExists()
        {
            const string source = """
                using EnumWrapper.SourceGenerators;

                namespace System.Windows.Data
                {
                    public interface IValueConverter
                    {
                        object? Convert(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture);
                        object? ConvertBack(object? value, System.Type targetType, object? parameter, System.Globalization.CultureInfo culture);
                    }
                }

                namespace Sample
                {
                    [GenerateEnumWrapper]
                    public enum PaymentState
                    {
                        Pending
                    }
                }
                """;

            var result = RunGenerator(source);
            AssertNoErrors(result);

            var generated = result.GetGeneratedSource("Sample.PaymentStateWrapper.g.cs");
            StringAssert.Contains(generated, "public class PaymentStateToWrapperConverter : System.Windows.Data.IValueConverter");
        }

        [TestMethod]
        public void GeneratorDriver_ReportsDuplicateTargetConfiguration()
        {
            const string source = """
                using EnumWrapper.SourceGenerators;

                [assembly: GenerateEnumWrapperFor(typeof(Sample.PaymentState))]

                namespace Sample;

                [GenerateEnumWrapper]
                public enum PaymentState
                {
                    Pending
                }
                """;

            var result = RunGenerator(source);
            AssertDiagnostic(result, "EWG003");
            Assert.AreEqual(1, result.GeneratedSources.Count(source => source.HintName.EndsWith("PaymentStateWrapper.g.cs", StringComparison.Ordinal)));
        }

        [TestMethod]
        public void GeneratorDriver_ReportsWrapperNameCollision()
        {
            const string source = """
                using EnumWrapper.SourceGenerators;

                namespace First
                {
                    [GenerateEnumWrapper(WrapperClassName = "SharedWrapper", CustomNamespace = "Contracts")]
                    public enum OrderStatus
                    {
                        Pending
                    }
                }

                namespace Second
                {
                    [GenerateEnumWrapper(WrapperClassName = "SharedWrapper", CustomNamespace = "Contracts")]
                    public enum InvoiceStatus
                    {
                        Pending
                    }
                }
                """;

            var result = RunGenerator(source);
            AssertDiagnostic(result, "EWG004");
        }

        private static TestGenerationResult RunGenerator(string source)
        {
            var parseOptions = new CSharpParseOptions(LanguageVersion.Latest);
            var syntaxTree = CSharpSyntaxTree.ParseText(source, parseOptions);
            var compilation = CSharpCompilation.Create(
                assemblyName: "GeneratorDriverTests",
                syntaxTrees: new[] { syntaxTree },
                references: GetMetadataReferences(),
                options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { new EnumWrapperSourceGenerator().AsSourceGenerator() }, parseOptions: parseOptions);
            driver = driver.RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out _);

            var runResult = driver.GetRunResult();
            return new TestGenerationResult(
                outputCompilation,
                outputCompilation.GetDiagnostics(),
                runResult.Results.Single().GeneratedSources);
        }

        private static ImmutableArray<MetadataReference> GetMetadataReferences()
        {
            var trustedAssemblies = AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") as string
                ?? throw new InvalidOperationException("Trusted platform assemblies are not available.");

            return trustedAssemblies
                .Split(Path.PathSeparator)
                .Select(path => MetadataReference.CreateFromFile(path))
                .Cast<MetadataReference>()
                .ToImmutableArray();
        }

        private static void AssertNoErrors(TestGenerationResult result)
        {
            var errors = result.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).ToArray();
            Assert.AreEqual(0, errors.Length, string.Join(Environment.NewLine, errors.Select(static d => d.ToString())));
        }

        private static void AssertDiagnostic(TestGenerationResult result, string diagnosticId)
        {
            if (!result.Diagnostics.Any(d => d.Id == diagnosticId))
            {
                Assert.Fail($"Expected diagnostic {diagnosticId}, but found:{Environment.NewLine}{string.Join(Environment.NewLine, result.Diagnostics.Select(static d => d.ToString()))}");
            }
        }

        private sealed record TestGenerationResult(
            Compilation OutputCompilation,
            ImmutableArray<Diagnostic> Diagnostics,
            ImmutableArray<GeneratedSourceResult> GeneratedSources)
        {
            public string GetGeneratedSource(string hintName)
            {
                foreach (var source in GeneratedSources)
                {
                    if (source.HintName.EndsWith(hintName, StringComparison.Ordinal))
                    {
                        return source.SourceText.ToString();
                    }
                }

                Assert.Fail($"Generated source '{hintName}' was not found.");
                return string.Empty;
            }
        }
    }
}
