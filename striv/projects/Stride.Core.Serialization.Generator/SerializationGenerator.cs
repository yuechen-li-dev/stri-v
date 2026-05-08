using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Text;

namespace Stride.Core.Serialization.Generator;

[Generator]
public sealed class SerializationGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var candidates = context.SyntaxProvider.ForAttributeWithMetadataName(
            "Stride.Core.DataContractAttribute",
            static (node, _) => node is TypeDeclarationSyntax,
            static (ctx, _) => (INamedTypeSymbol)ctx.TargetSymbol);

        var compilationAndTypes = context.CompilationProvider.Combine(candidates.Collect());
        context.RegisterSourceOutput(compilationAndTypes, Generate);
    }

    private static void Generate(SourceProductionContext context, (Compilation Left, ImmutableArray<INamedTypeSymbol> Right) input)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        foreach (var type in input.Right)
        {
            var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
            if (seen.Add(key))
                EmitForType(context, type);
        }
    }

    private static void EmitForType(SourceProductionContext context, INamedTypeSymbol type)
    {
        if (type.TypeArguments.Length != 0)
        {
            context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("STRISG001", "Unsupported data contract", "Generic DataContract types are not supported in M11c MVP", "Stride.Serialization", DiagnosticSeverity.Error, true), type.Locations.FirstOrDefault()));
            return;
        }

        var members = new List<(int order, ISymbol symbol, ITypeSymbol memberType)>();
        foreach (var member in type.GetMembers())
        {
            if (member.GetAttributes().Any(a => a.AttributeClass?.ToDisplayString() == "Stride.Core.DataMemberIgnoreAttribute"))
                continue;

            var dataMember = member.GetAttributes().FirstOrDefault(a => a.AttributeClass?.ToDisplayString() == "Stride.Core.DataMemberAttribute");
            if (dataMember == null)
                continue;

            if (member.DeclaredAccessibility is not (Accessibility.Public or Accessibility.Internal))
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("STRISG002", "Unsupported member", $"Member '{member.Name}' must be public/internal", "Stride.Serialization", DiagnosticSeverity.Error, true), member.Locations.FirstOrDefault()));
                continue;
            }

            var order = dataMember.ConstructorArguments.Length > 0 ? (int)dataMember.ConstructorArguments[0].Value! : int.MaxValue;
            ITypeSymbol? memberType = member switch
            {
                IFieldSymbol f => f.Type,
                IPropertySymbol p when p.GetMethod != null && p.SetMethod != null => p.Type,
                _ => null,
            };

            if (memberType == null || !IsSupportedPrimitive(memberType))
            {
                context.ReportDiagnostic(Diagnostic.Create(new DiagnosticDescriptor("STRISG003", "Unsupported member type", $"Member '{member.Name}' type is unsupported in M11c MVP", "Stride.Serialization", DiagnosticSeverity.Error, true), member.Locations.FirstOrDefault()));
                continue;
            }

            members.Add((order, member, memberType));
        }

        members.Sort((a, b) => a.order.CompareTo(b.order));

        var fqType = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty);
        var serializerName = type.Name + "DataSerializer";
        var hint = type.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat).Replace('<', '_').Replace('>', '_').Replace('.', '_') + ".Serialization.g.cs";

        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Reflection;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using Stride.Core.Serialization;");
        sb.AppendLine("using Stride.Core.Storage;");
        sb.AppendLine("namespace Stride.Core.DataSerializers;");
        sb.AppendLine($"internal sealed class {serializerName} : DataSerializer<{fqType}>");
        sb.AppendLine("{");

        foreach (var (order, symbol, memberType) in members)
            sb.AppendLine($"    private DataSerializer<{memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty)}> {symbol.Name}Serializer = null!;");

        sb.AppendLine("    public override void Initialize(SerializerSelector serializerSelector)");
        sb.AppendLine("    {");
        foreach (var (_, symbol, memberType) in members)
            sb.AppendLine($"        {symbol.Name}Serializer = MemberSerializer<{memberType.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat).Replace("global::", string.Empty)}>.Create(serializerSelector);");
        sb.AppendLine("    }");
        sb.AppendLine($"    public override void PreSerialize(ref {fqType} obj, ArchiveMode mode, SerializationStream stream)");
        sb.AppendLine("    {");
        if (type.IsReferenceType)
            sb.AppendLine("        if (mode == ArchiveMode.Deserialize && obj == null) obj = new();");
        sb.AppendLine("    }");
        sb.AppendLine($"    public override void Serialize(ref {fqType} obj, ArchiveMode mode, SerializationStream stream)");
        sb.AppendLine("    {");
        sb.AppendLine("        Initialize(stream.Context.SerializerSelector);");
        if (type.IsReferenceType)
            sb.AppendLine("        if (mode == ArchiveMode.Deserialize && obj == null) obj = new();");
        foreach (var (_, symbol, _) in members)
        {
            sb.AppendLine($"        var {symbol.Name}Value = obj.{symbol.Name};");
            sb.AppendLine($"        {symbol.Name}Serializer.Serialize(ref {symbol.Name}Value, mode, stream);");
            sb.AppendLine($"        if (mode == ArchiveMode.Deserialize) obj.{symbol.Name} = {symbol.Name}Value;");
        }
        sb.AppendLine("    }");
        sb.AppendLine("}");
        sb.AppendLine("internal static class GeneratedSerializationRegistrar");
        sb.AppendLine("{");
        sb.AppendLine("    [ModuleInitializer]");
        sb.AppendLine("    internal static void Register() => RegisterForTests();");
        sb.AppendLine("    internal static void RegisterForTests()");
        sb.AppendLine("    {");
        sb.AppendLine("        var assembly = typeof(GeneratedSerializationRegistrar).Assembly;");
        sb.AppendLine("        var serializers = DataSerializerFactory.GetAssemblySerializers(assembly) ?? new AssemblySerializers(assembly);");
        sb.AppendLine("        if (!serializers.Profiles.TryGetValue(\"Default\", out var profile)) { profile = new AssemblySerializersPerProfile(); serializers.Profiles.Add(\"Default\", profile); }");
        sb.AppendLine($"        profile.Add(new AssemblySerializerEntry(ObjectId.FromBytes(System.Text.Encoding.UTF8.GetBytes(\"{fqType}\")), typeof({fqType}), typeof({serializerName})));\n        DataSerializerFactory.RegisterSerializationAssembly(serializers);");
        sb.AppendLine("    }");
        sb.AppendLine("}");

        context.AddSource(hint, SourceText.From(sb.ToString(), Encoding.UTF8));
    }

    private static bool IsSupportedPrimitive(ITypeSymbol type)
    {
        return type.SpecialType is SpecialType.System_Int32 or SpecialType.System_Single or SpecialType.System_Boolean or SpecialType.System_String;
    }
}
