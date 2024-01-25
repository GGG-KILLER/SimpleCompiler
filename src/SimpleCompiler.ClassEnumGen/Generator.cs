﻿using System.CodeDom.Compiler;
using System.Collections.Immutable;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace SimpleCompiler.ClassEnumGen;

[Generator(LanguageNames.CSharp)]
public class Generator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        context.RegisterPostInitializationOutput(ctx =>
        {
            ctx.AddSource(CodeConstants.ClassEnumAttribute.FullName + ".g.cs", CodeConstants.ClassEnumAttribute.Code);
        });

        var classes = context.SyntaxProvider.ForAttributeWithMetadataName(
            CodeConstants.Namespace + "." + CodeConstants.ClassEnumAttribute.FullName,
            (n, _) => n is RecordDeclarationSyntax,
            (ctx, _) =>
            {
                var attr = ctx.Attributes.Single();
                var baseType = (INamedTypeSymbol)ctx.TargetSymbol;
                var name = attr.NamedArguments.SingleOrDefault(x => x.Key == "KindEnumName").Value.Value as string
                    ?? baseType.Name + "Kind";

                var values = ImmutableArray.CreateBuilder<EnumValue>();
                foreach (var member in baseType.GetMembers()
                                               .OfType<IMethodSymbol>()
                                               .Where(x => x.IsStatic
                                                           && x.DeclaredAccessibility is Accessibility.Public
                                                                                      or Accessibility.Internal
                                                                                      or Accessibility.ProtectedOrInternal
                                                           && x.IsPartialDefinition
                                                           && x.Locations.Any(x => x.IsInSource)))
                {
                    values.Add(new EnumValue(
                        member.Name,
                        member.DeclaredAccessibility,
                        member.ReturnType,
                        member.Parameters
                    ));
                }

                return new ClassEnum(
                    baseType,
                    name,
                    values.ToImmutable());
            }
        );

        context.RegisterSourceOutput(classes, (ctx, eclass) =>
        {
            var builder = new StringBuilder();
            var writer = new IndentedTextWriter(new StringWriter(builder));

            writer.WriteLine("// <auto-generated />");
            writer.WriteLine();
            writer.WriteLine("#nullable enable");
            writer.WriteLine();

            writer.WriteLine($"namespace {eclass.Base.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.CSharpErrorMessageFormat)};");

            var modifier = eclass.Base.DeclaredAccessibility switch
            {
                Accessibility.Private => "private",
                Accessibility.ProtectedAndInternal => "private protected",
                Accessibility.Protected => "protected",
                Accessibility.Internal => "internal",
                Accessibility.ProtectedOrInternal => "protected internal",
                Accessibility.Public => "public",
                _ => throw new NotImplementedException(),
            };

            #region EnumKind
            {
                writer.Write(modifier);
                writer.Write(' ');
                writer.Write("enum ");
                writer.WriteLine(eclass.EnumValuesKindName);
                writer.WriteLine('{');
                writer.Indent++;

                foreach (var value in eclass.Values)
                {
                    writer.Write(value.Name);
                    writer.WriteLine(',');
                }

                writer.Indent--;
                writer.WriteLine('}');
            }
            #endregion EnumKind

            #region Base Type Override
            {
                writer.WriteLine();
                writer.Write(eclass.Base.IsRecord ? "partial record " : "partial class ");
                writer.WriteLine(eclass.Base.Name);
                writer.WriteLine('{');
                writer.Indent++;
                foreach (var value in eclass.Values)
                {
                    var mod = value.Accessibility switch
                    {
                        Accessibility.Private => "private",
                        Accessibility.ProtectedAndInternal => "private protected",
                        Accessibility.Protected => "protected",
                        Accessibility.Internal => "internal",
                        Accessibility.ProtectedOrInternal => "protected internal",
                        Accessibility.Public => "public",
                        _ => throw new NotImplementedException(),
                    };

                    writer.Write(mod);
                    writer.Write(" static partial ");
                    writer.Write(value.ReturnType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    writer.Write(' ');
                    writer.Write(value.Name);
                    writer.Write('(');
                    var first = true;
                    foreach (var arg in value.Arguments)
                    {
                        if (!first) writer.Write(", ");
                        first = false;
                        writer.Write(arg.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        writer.Write(' ');
                        writer.Write(arg.Name);
                    }
                    writer.Write(") => ");
                    if (value.Arguments.Any())
                    {
                        writer.Write("new(");
                        first = true;
                        foreach (var arg in value.Arguments)
                        {
                            if (!first) writer.Write(", ");
                            first = false;
                            writer.Write(arg.Name);
                        }
                        writer.WriteLine(");");
                    }
                    else
                    {
                        writer.Write(eclass.Base.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                        writer.Write('.');
                        writer.Write(value.ReturnType.Name);
                        writer.Write('.');
                        writer.WriteLine("Instance;");
                    }
                }
                writer.Indent--;
                writer.WriteLine('}');
            }
            #endregion Base Type Override

            #region Enum Value Types
            foreach (var value in eclass.Values)
            {
                writer.WriteLine();
                writer.Write(modifier);
                writer.Write(eclass.Base.IsRecord ? " sealed partial record " : " sealed partial class ");
                writer.Write(value.ReturnType.Name);
                writer.Write('(');
                var first = true;
                foreach (var arg in value.Arguments)
                {
                    if (!first) writer.Write(", ");
                    first = false;
                    writer.Write(arg.Type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                    writer.Write(' ');
                    writer.Write(char.ToUpperInvariant(arg.Name[0]));
                    writer.Write(arg.Name.Substring(1));
                }
                writer.Write(") : ");
                writer.Write(eclass.Base.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                writer.Write('(');
                writer.Write(eclass.Base.ContainingNamespace.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat));
                writer.Write('.');
                writer.Write(eclass.EnumValuesKindName);
                writer.Write('.');
                writer.Write(value.Name);
                writer.Write(')');

                if (value.Arguments.Any())
                {
                    writer.WriteLine(';');
                }
                else
                {
                    writer.WriteLine();
                    writer.WriteLine('{');
                    writer.Indent++;
                    writer.Write("public static readonly ");
                    writer.Write(value.ReturnType.Name);
                    writer.WriteLine(" Instance = new();");
                    writer.Indent--;
                    writer.WriteLine('}');
                }
            }
            #endregion Enum Value Types

            writer.Flush();
            ctx.AddSource($"{eclass.Base.Name}.g.cs", builder.ToString());
        });
    }
}

internal sealed record ClassEnum(INamedTypeSymbol Base, string EnumValuesKindName, IEnumerable<EnumValue> Values);
public sealed class ClassEnumComparer : IEqualityComparer<ClassEnum>
{
    public static readonly ClassEnumComparer Instance = new();

    bool IEqualityComparer<ClassEnum>.Equals(ClassEnum x, ClassEnum y)
    {
        return (x is null == y is null)
            && (x is null || (
                SymbolEqualityComparer.Default.Equals(x!.Base, y!.Base)
                && string.Equals(x.EnumValuesKindName, y.EnumValuesKindName, StringComparison.Ordinal)
                && x.Values.SequenceEqual(y.Values)));
    }

    int IEqualityComparer<ClassEnum>.GetHashCode(ClassEnum obj)
    {
        var hash = new HashCode();
        hash.Add(obj.Base, SymbolEqualityComparer.Default);
        hash.Add(obj.EnumValuesKindName, StringComparer.Ordinal);
        foreach (var value in obj.Values)
            hash.Add(value, EnumValueComparer.Instance);
        return hash.ToHashCode();
    }
}

internal sealed record EnumValue(string Name, Accessibility Accessibility, ITypeSymbol ReturnType, IEnumerable<IParameterSymbol> Arguments);
public sealed class EnumValueComparer : IEqualityComparer<EnumValue>
{
    public static EnumValueComparer Instance = new();

    bool IEqualityComparer<EnumValue>.Equals(EnumValue x, EnumValue y)
    {
        return (x is null == y is null)
            && (x is null || (
                string.Equals(x.Name, y!.Name, StringComparison.Ordinal)
                && x.Accessibility == y.Accessibility
                && SymbolEqualityComparer.Default.Equals(x.ReturnType, y.ReturnType)
                && x.Arguments.SequenceEqual(y.Arguments, SymbolEqualityComparer.Default)
            ));
    }

    int IEqualityComparer<EnumValue>.GetHashCode(EnumValue obj)
    {
        var hash = new HashCode();
        hash.Add(obj.ReturnType, SymbolEqualityComparer.Default);
        foreach (var argument in obj.Arguments)
            hash.Add(argument, SymbolEqualityComparer.Default);
        return hash.ToHashCode();
    }
}

internal static class CodeConstants
{
    public const string Namespace = "ClassEnumGen";

    public static class ClassEnumAttribute
    {
        public const string FullName = "ClassEnumAttribute";

        public const string Code = $$"""
        using System;

        #nullable enable

        namespace {{Namespace}};

        [AttributeUsage(AttributeTargets.All, Inherited = false, AllowMultiple = true)]
        public sealed class {{FullName}} : Attribute
        {
            public string? KindEnumName { get; set; }
        }
        """;
    }
}