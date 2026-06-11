using System.Collections.Generic;
using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace EnumWrapper.SourceGenerators
{
    internal static class GenerationPlanner
    {
        public static void EmitSources(
            SourceProductionContext context,
            ImmutableArray<GeneratorResult<EnumToWrapInfo>> localResults,
            ImmutableArray<GeneratorResult<EnumToWrapInfo>> assemblyResults)
        {
            var validInfos = new List<EnumToWrapInfo>();

            CollectResults(context, localResults, validInfos);
            CollectResults(context, assemblyResults, validInfos);

            var seenTargets = new HashSet<string>(System.StringComparer.Ordinal);
            var seenWrappers = new Dictionary<string, EnumToWrapInfo>(System.StringComparer.Ordinal);

            foreach (var info in validInfos)
            {
                var wrapperKey = $"{info.WrapperNamespace}.{info.WrapperClassName}";
                var targetKey = $"{info.EnumFullName}|{wrapperKey}";

                if (!seenTargets.Add(targetKey))
                {
                    ReportDiagnostic(
                        context,
                        new DiagnosticInfo(
                            "EWG003",
                            $"The enum '{info.EnumFullName}' is configured multiple times to generate wrapper '{wrapperKey}'. Remove the duplicate wrapper declaration.",
                            DiagnosticSeverity.Error,
                            info.DeclarationLocation));
                    continue;
                }

                if (seenWrappers.TryGetValue(wrapperKey, out var existing) && existing.EnumFullName != info.EnumFullName)
                {
                    ReportDiagnostic(
                        context,
                        new DiagnosticInfo(
                            "EWG004",
                            $"The wrapper type '{wrapperKey}' is generated for both '{existing.EnumFullName}' and '{info.EnumFullName}'. Generated wrapper types must have unique fully qualified names.",
                            DiagnosticSeverity.Error,
                            info.DeclarationLocation));
                    continue;
                }

                seenWrappers[wrapperKey] = info;
                CodeWriter.GenerateWrapper(context, info);
            }
        }

        private static void CollectResults(
            SourceProductionContext context,
            ImmutableArray<GeneratorResult<EnumToWrapInfo>> results,
            List<EnumToWrapInfo> validInfos)
        {
            foreach (var result in results)
            {
                if (result.Diagnostic != null)
                {
                    ReportDiagnostic(context, result.Diagnostic);
                    continue;
                }

                if (result.Value != null)
                {
                    validInfos.Add(result.Value);
                }
            }
        }

        private static void ReportDiagnostic(SourceProductionContext context, DiagnosticInfo info)
        {
            var descriptor = new DiagnosticDescriptor(
                info.Id,
                "EnumWrapper.SourceGenerators Error",
                info.Message,
                "Usage",
                info.Severity,
                isEnabledByDefault: true);

            context.ReportDiagnostic(Diagnostic.Create(descriptor, info.Location));
        }
    }
}
