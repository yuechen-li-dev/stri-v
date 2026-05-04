using System.Text.RegularExpressions;
using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;

namespace StriV.ShaderPipeline.Tests;

public class GenericSpecializationTests
{
    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    private static string ReadCombinedSpritePair()
        => ReadFixture("sdsl/sprite/SpriteBase.sdsl") + "\n" + ReadFixture("sdsl/sprite/SpriteBatchShader.sdsl");

    [Fact]
    public void GenericParser_ParsesBoolParameter()
    {
        var doc = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair()).Document!;
        var shader = Assert.Single(doc.Shaders.Where(s => s.Name == "SpriteBatchShader"));
        var param = Assert.Single(shader.GenericParameters);
        Assert.Equal("bool", param.TypeText);
        Assert.Equal("TSRgb", param.Name);
    }

    [Fact]
    public void GenericSpecialization_ReplacesStandaloneBoolIdentifier()
    {
        var input = "TSRgb TSRgb2 \"TSRgb\" // TSRgb";
        var output = ShaderLowerer.SubstituteIdentifiers(input, new Dictionary<string, string> { ["TSRgb"] = "false" });
        Assert.StartsWith("false TSRgb2", output, StringComparison.Ordinal);
        Assert.Contains("\"TSRgb\"", output);
        Assert.Contains("// TSRgb", output);
    }

    [Fact]
    public void SpriteBatchSpecialization_False_RemovesUnsupportedGenericDiagnostic()
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(parsed.Document!, "SpriteBatchShader", new ShaderSpecialization(new Dictionary<string, bool>{{"TSRgb", false}}));
        Assert.DoesNotContain(lowered.Diagnostics, d => d.Code == "SD320" || d.Code == "SD301");
        Assert.DoesNotMatch(new Regex(@"\bTSRgb\b"), lowered.Hlsl);
    }

    [Fact]
    public void SpriteBatchSpecialization_True_ReplacesWithTrue()
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(parsed.Document!, "SpriteBatchShader", new ShaderSpecialization(new Dictionary<string, bool>{{"TSRgb", true}}));
        Assert.Contains("true", lowered.Hlsl);
    }

    [Fact]
    public void SpriteBatchSpecialization_MissingValue_DiagnosesMissingSpecialization()
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(parsed.Document!, "SpriteBatchShader");
        Assert.Contains(lowered.Diagnostics, d => d.Code == "SD320");
    }

    [Fact]
    public void Specialization_UnknownKey_DiagnosesUnknownParameter()
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(parsed.Document!, "SpriteBatchShader", new ShaderSpecialization(new Dictionary<string, bool>{{"NotAParam", true}}));
        Assert.Contains(lowered.Diagnostics, d => d.Code == "SD322");
    }
}
