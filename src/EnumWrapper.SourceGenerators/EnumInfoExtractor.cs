using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumWrapper.SourceGenerators
{
    internal static class EnumInfoExtractor
    {
        public static GeneratorResult<EnumToWrapInfo>? GetEnumToWrap(GeneratorAttributeSyntaxContext ctx, CancellationToken cancellationToken)
        {
            if (ctx.TargetSymbol is not INamedTypeSymbol symbol) return null;

            var targetAttr = ctx.Attributes[0];

            // EWG001: Applied to a non-enum type
            if (symbol.TypeKind != TypeKind.Enum)
            {
                var location = symbol.Locations.FirstOrDefault() ?? Location.None;
                var diag = new DiagnosticInfo(
                    "EWG001",
                    $"The [GenerateEnumWrapper] attribute can only be applied to enum types. '{symbol.Name}' is a {symbol.TypeKind.ToString().ToLower()}.",
                    DiagnosticSeverity.Error,
                    location
                );
                return new GeneratorResult<EnumToWrapInfo>(diag);
            }

            string? wrapperClassName = null;
            string? customNamespace = null;
            foreach (var namedArg in targetAttr.NamedArguments)
            {
                if (namedArg.Key == "WrapperClassName")
                    wrapperClassName = namedArg.Value.Value as string;
                else if (namedArg.Key == "CustomNamespace")
                    customNamespace = namedArg.Value.Value as string;
            }

            var compilation = ctx.SemanticModel.Compilation;
            var hasJson = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonConverterAttribute") != null;
            var hasWpf = compilation.GetTypeByMetadataName("System.Windows.Data.IValueConverter") != null;

            var info = ExtractEnumInfo(
                symbol,
                wrapperClassName,
                customNamespace,
                hasJson,
                hasWpf,
                symbol.Locations.FirstOrDefault() ?? Location.None);
            return new GeneratorResult<EnumToWrapInfo>(info);
        }

        public static ImmutableArray<GeneratorResult<EnumToWrapInfo>> GetAssemblyEnumsToWrap(Compilation compilation, CancellationToken cancellationToken)
        {
            var list = ImmutableArray.CreateBuilder<GeneratorResult<EnumToWrapInfo>>();
            var attributes = compilation.Assembly.GetAttributes();
            var hasJson = compilation.GetTypeByMetadataName("System.Text.Json.Serialization.JsonConverterAttribute") != null;
            var hasWpf = compilation.GetTypeByMetadataName("System.Windows.Data.IValueConverter") != null;

            foreach (var attr in attributes)
            {
                if (cancellationToken.IsCancellationRequested) break;

                if (attr.AttributeClass is { } attrClass &&
                    attrClass.ToDisplayString() == "EnumWrapper.SourceGenerators.GenerateEnumWrapperForAttribute")
                {
                    if (attr.ConstructorArguments.Length > 0)
                    {
                        var typeArg = attr.ConstructorArguments[0].Value as INamedTypeSymbol;

                        // EWG002: Targeted type is not an enum
                        if (typeArg == null || typeArg.TypeKind != TypeKind.Enum)
                        {
                            var location = attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None;
                            var targetedName = typeArg?.Name ?? "unknown";
                            var diag = new DiagnosticInfo(
                                "EWG002",
                                $"The targeted type of GenerateEnumWrapperFor must be an enum. '{targetedName}' is not an enum.",
                                DiagnosticSeverity.Error,
                                location
                            );
                            list.Add(new GeneratorResult<EnumToWrapInfo>(diag));
                            continue;
                        }

                        string? wrapperClassName = null;
                        string? customNamespace = null;
                        foreach (var namedArg in attr.NamedArguments)
                        {
                            if (namedArg.Key == "WrapperClassName")
                                wrapperClassName = namedArg.Value.Value as string;
                            else if (namedArg.Key == "CustomNamespace")
                                customNamespace = namedArg.Value.Value as string;
                        }

                        var info = ExtractEnumInfo(
                            typeArg,
                            wrapperClassName,
                            customNamespace,
                            hasJson,
                            hasWpf,
                            attr.ApplicationSyntaxReference?.GetSyntax(cancellationToken).GetLocation() ?? Location.None);
                        list.Add(new GeneratorResult<EnumToWrapInfo>(info));
                    }
                }
            }
            return list.ToImmutable();
        }

        public static EnumToWrapInfo ExtractEnumInfo(
            INamedTypeSymbol enumSymbol,
            string? wrapperClassName,
            string? customNamespace,
            bool hasJson,
            bool hasWpf,
            Location declarationLocation)
        {
            var enumName = enumSymbol.Name;
            var enumNamespace = enumSymbol.ContainingNamespace.IsGlobalNamespace
                ? ""
                : enumSymbol.ContainingNamespace.ToDisplayString();

            var enumFullName = enumSymbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);

            var targetNamespace = customNamespace ?? enumNamespace;
            if (string.IsNullOrEmpty(targetNamespace))
            {
                targetNamespace = "EnumWrapper.SourceGenerators.Generated";
            }

            var targetClassName = wrapperClassName ?? $"{enumName}Wrapper";

            // Detect underlying type
            var underlyingTypeName = "int";
            var isUnsignedUnderlyingType = false;
            var underlyingType = enumSymbol.EnumUnderlyingType;
            if (underlyingType != null)
            {
                underlyingTypeName = underlyingType.SpecialType switch
                {
                    SpecialType.System_Byte => "byte",
                    SpecialType.System_SByte => "sbyte",
                    SpecialType.System_Int16 => "short",
                    SpecialType.System_UInt16 => "ushort",
                    SpecialType.System_Int32 => "int",
                    SpecialType.System_UInt32 => "uint",
                    SpecialType.System_Int64 => "long",
                    SpecialType.System_UInt64 => "ulong",
                    _ => underlyingType.ToDisplayString()
                };

                isUnsignedUnderlyingType = underlyingType.SpecialType switch
                {
                    SpecialType.System_Byte => true,
                    SpecialType.System_UInt16 => true,
                    SpecialType.System_UInt32 => true,
                    SpecialType.System_UInt64 => true,
                    _ => false
                };
            }

            // Detect Flags attribute
            var isFlags = enumSymbol.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "System.FlagsAttribute");

            // Extract original XML docs
            var enumXmlDocs = enumSymbol.GetDocumentationCommentXml() ?? "";

            var members = new List<EnumMemberInfo>();
            foreach (var member in enumSymbol.GetMembers())
            {
                if (member is IFieldSymbol field && field.ConstantValue != null)
                {
                    var description = field.Name;
                    string? shortName = null;
                    int? order = null;
                    string? groupName = null;

                    foreach (var attr in field.GetAttributes())
                    {
                        var attrClassName = attr.AttributeClass?.ToDisplayString();
                        if (attrClassName == "System.ComponentModel.DescriptionAttribute")
                        {
                            if (attr.ConstructorArguments.Length > 0 && attr.ConstructorArguments[0].Value is string desc)
                            {
                                description = desc;
                            }
                        }
                        else if (attrClassName == "System.ComponentModel.DataAnnotations.DisplayAttribute")
                        {
                            foreach (var namedArg in attr.NamedArguments)
                            {
                                if (namedArg.Key == "Name" && namedArg.Value.Value is string name)
                                {
                                    description = name;
                                }
                                else if (namedArg.Key == "ShortName" && namedArg.Value.Value is string sName)
                                {
                                    shortName = sName;
                                }
                                else if (namedArg.Key == "GroupName" && namedArg.Value.Value is string group)
                                {
                                    groupName = group;
                                }
                                else if (namedArg.Key == "Order" && namedArg.Value.Value is int ord)
                                {
                                    order = ord;
                                }
                            }
                        }
                    }

                    // Safe numeric conversion: handle ulong values that exceed long.MaxValue
                    var numericVal = GetNumericValue(field.ConstantValue);

                    var fieldXmlDocs = field.GetDocumentationCommentXml() ?? "";
                    members.Add(new EnumMemberInfo(field.Name, description, shortName, order, groupName, numericVal, fieldXmlDocs));
                }
            }

            // Perform Compile-Time Sorting: Order by 'Order' ascending (null values are sorted last)
            var sortedMembers = members
                .OrderBy(m => m.Order ?? int.MaxValue)
                .ToImmutableArray();

            // Detect contiguity (Flags enums are naturally not contiguous)
            bool isContiguous = true;
            decimal minNumericValue = 0;
            if (sortedMembers.Length == 0 || isFlags)
            {
                isContiguous = false;
            }
            else
            {
                // Sort by numeric value to check contiguity
                var sortedByValue = sortedMembers.OrderBy(m => m.NumericValue).ToArray();
                minNumericValue = sortedByValue[0].NumericValue;
                for (int i = 0; i < sortedByValue.Length; i++)
                {
                    if (sortedByValue[i].NumericValue != minNumericValue + i)
                    {
                        isContiguous = false;
                        break;
                    }
                }
            }

            return new EnumToWrapInfo(
                enumNamespace,
                enumName,
                enumFullName,
                targetClassName,
                targetNamespace,
                sortedMembers,
                underlyingTypeName,
                isUnsignedUnderlyingType,
                isFlags,
                hasJson,
                isContiguous,
                minNumericValue,
                enumXmlDocs,
                hasWpf,
                declarationLocation
            );
        }

        private static decimal GetNumericValue(object constantValue)
        {
            return constantValue switch
            {
                byte value => value,
                sbyte value => value,
                short value => value,
                ushort value => value,
                int value => value,
                uint value => value,
                long value => value,
                ulong value => value,
                _ => throw new InvalidOperationException($"Unsupported enum constant type '{constantValue.GetType().FullName}'.")
            };
        }
    }
}
