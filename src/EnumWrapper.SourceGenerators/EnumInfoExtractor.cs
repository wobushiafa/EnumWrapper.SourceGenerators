using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EnumWrapper.SourceGenerators
{
    internal static class EnumInfoExtractor
    {
        public static GeneratorResult<EnumToWrapInfo>? GetEnumToWrap(GeneratorSyntaxContext ctx, CancellationToken cancellationToken)
        {
            var typeDeclaration = (BaseTypeDeclarationSyntax)ctx.Node;
            var symbol = ctx.SemanticModel.GetDeclaredSymbol(typeDeclaration, cancellationToken) as INamedTypeSymbol;
            if (symbol is null) return null;

            AttributeData? targetAttr = null;
            foreach (var attr in symbol.GetAttributes())
            {
                if (attr.AttributeClass is { } attrClass &&
                    attrClass.ToDisplayString() == "EnumWrapper.SourceGenerators.GenerateEnumWrapperAttribute")
                {
                    targetAttr = attr;
                    break;
                }
            }

            if (targetAttr == null) return null;

            // EWG001: Applied to a non-enum type
            if (symbol.TypeKind != TypeKind.Enum)
            {
                var location = typeDeclaration.Identifier.GetLocation();
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

            var info = ExtractEnumInfo(symbol, wrapperClassName, customNamespace, hasJson, hasWpf);
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

                        var info = ExtractEnumInfo(typeArg, wrapperClassName, customNamespace, hasJson, hasWpf);
                        list.Add(new GeneratorResult<EnumToWrapInfo>(info));
                    }
                }
            }
            return list.ToImmutable();
        }

        public static EnumToWrapInfo ExtractEnumInfo(INamedTypeSymbol enumSymbol, string? wrapperClassName, string? customNamespace, bool hasJson, bool hasWpf)
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
                    long numericVal;
                    if (field.ConstantValue is ulong ulongVal)
                        numericVal = unchecked((long)ulongVal);
                    else
                        numericVal = Convert.ToInt64(field.ConstantValue);

                    var fieldXmlDocs = field.GetDocumentationCommentXml() ?? "";
                    members.Add(new EnumMemberInfo(field.Name, description, shortName, order, groupName, numericVal, fieldXmlDocs));
                }
            }

            // Perform Compile-Time Sorting: Order by 'Order' ascending (null values are sorted last)
            var sortedMembers = members
                .OrderBy(m => m.Order ?? int.MaxValue)
                .ToImmutableArray();

            // Detect contiguity starting from 0 (Flags enums are naturally not contiguous)
            bool isContiguous = true;
            if (sortedMembers.Length == 0 || isFlags)
            {
                isContiguous = false;
            }
            else
            {
                // Sort by numeric value to check contiguity
                var sortedByValue = sortedMembers.OrderBy(m => m.NumericValue).ToArray();
                for (int i = 0; i < sortedByValue.Length; i++)
                {
                    if (sortedByValue[i].NumericValue != i)
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
                isFlags,
                hasJson,
                isContiguous,
                enumXmlDocs,
                hasWpf
            );
        }
    }
}
