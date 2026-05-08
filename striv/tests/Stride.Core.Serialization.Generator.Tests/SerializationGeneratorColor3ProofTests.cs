using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System.Reflection;
using Xunit;

namespace Stride.Core.Serialization.Generator.Tests;

public class SerializationGeneratorColor3ProofTests
{
    [Fact]
    public void Generator_EmitsSerializer_ForRealColor3ContractShape_SourceSubset()
    {
        var repoRoot = FindRepositoryRoot();
        var color3Path = Path.Combine(repoRoot, "striv/projects/Stride.Core.Mathematics/Color3.cs");
        var color3Text = File.ReadAllText(color3Path);

        Assert.Contains("[DataContract(\"Color3\")]", color3Text, StringComparison.Ordinal);
        Assert.Contains("[DataMember(0)]", color3Text, StringComparison.Ordinal);
        Assert.Contains("public float R;", color3Text, StringComparison.Ordinal);
        Assert.Contains("[DataMember(1)]", color3Text, StringComparison.Ordinal);
        Assert.Contains("public float G;", color3Text, StringComparison.Ordinal);
        Assert.Contains("[DataMember(2)]", color3Text, StringComparison.Ordinal);
        Assert.Contains("public float B;", color3Text, StringComparison.Ordinal);

        const string source = """
using Stride.Core;

namespace Stride.Core.Mathematics
{
[DataContract("Color3")]
public struct Color3
{
    [DataMember(0)] public float R;
    [DataMember(1)] public float G;
    [DataMember(2)] public float B;
}
}

namespace Stride.Core
{

[System.AttributeUsage(System.AttributeTargets.Class | System.AttributeTargets.Struct)]
public sealed class DataContractAttribute : System.Attribute
{
    public DataContractAttribute(string? name = null) { }
}

[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
public sealed class DataMemberAttribute : System.Attribute
{
    public DataMemberAttribute(int order) { }
}

[System.AttributeUsage(System.AttributeTargets.Field | System.AttributeTargets.Property)]
public sealed class DataMemberIgnoreAttribute : System.Attribute
{
}
}
""";

        var compilation = CSharpCompilation.Create(
            "Color3Proof",
            new[] { CSharpSyntaxTree.ParseText(source) },
            new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Runtime.CompilerServices.ModuleInitializerAttribute).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
            },
            new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

        var generator = LoadGenerator();
        GeneratorDriver driver = CSharpGeneratorDriver.Create(generator);
        driver = driver.RunGenerators(compilation);

        var runResult = driver.GetRunResult();
        Assert.Empty(runResult.Diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error));

        var generatedSource = runResult.Results[0].GeneratedSources.Single().SourceText.ToString();

        Assert.Contains("class Color3DataSerializer", generatedSource, StringComparison.Ordinal);
        Assert.Contains("var RValue = obj.R;", generatedSource, StringComparison.Ordinal);
        Assert.Contains("var GValue = obj.G;", generatedSource, StringComparison.Ordinal);
        Assert.Contains("var BValue = obj.B;", generatedSource, StringComparison.Ordinal);

        var rIndex = generatedSource.IndexOf("var RValue = obj.R;", StringComparison.Ordinal);
        var gIndex = generatedSource.IndexOf("var GValue = obj.G;", StringComparison.Ordinal);
        var bIndex = generatedSource.IndexOf("var BValue = obj.B;", StringComparison.Ordinal);
        Assert.True(rIndex < gIndex && gIndex < bIndex, "Expected R,G,B serialization order.");
    }

    private static ISourceGenerator LoadGenerator()
    {
        var repoRoot = FindRepositoryRoot();
        var generatorPath = Path.Combine(repoRoot, "striv/projects/Stride.Core.Serialization.Generator/bin/Debug/netstandard2.0/Stride.Core.Serialization.Generator.dll");
        var generatorAssembly = Assembly.LoadFrom(generatorPath);
        var generatorType = generatorAssembly.GetType("Stride.Core.Serialization.Generator.SerializationGenerator")!;
        var instance = Activator.CreateInstance(generatorType)!;
        var asSourceGenerator = generatorType.GetMethod("AsSourceGenerator", BindingFlags.Public | BindingFlags.Static);
        return asSourceGenerator != null
            ? (ISourceGenerator)asSourceGenerator.Invoke(null, new[] { instance })!
            : ((IIncrementalGenerator)instance).AsSourceGenerator();
    }

    private static string FindRepositoryRoot()
    {
        var current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (Directory.Exists(Path.Combine(current, "striv", "projects")))
                return current;

            current = Directory.GetParent(current)?.FullName ?? string.Empty;
        }

        throw new InvalidOperationException("Repository root with 'striv/projects' not found.");
    }
}
