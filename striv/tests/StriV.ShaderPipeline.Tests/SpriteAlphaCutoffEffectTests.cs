using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;

namespace StriV.ShaderPipeline.Tests;

public class SpriteAlphaCutoffEffectTests
{
    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    private static string ReadCombinedSource()
        => ReadFixture("sdsl/sprite/SpriteBase.sdsl") + "\n" + ReadFixture("sdsl/sprite/SpriteAlphaCutoff.sdsl");

    [Fact]
    public void SpriteAlphaCutoff_Parse_FindsShaderAndEffectBlock()
    {
        var result = new ShaderParser().ParseSdslDocument(ReadCombinedSource());
        Assert.NotNull(result.Document);
        Assert.Contains(result.Document!.Shaders, s => s.Name == "SpriteAlphaCutoff");
        var effect = Assert.Single(result.Document.EffectBlocks, e => e.EffectName == "SpriteAlphaCutoffEffect");
        Assert.Equal("Stride.Rendering", effect.NamespaceName);
    }

    [Fact]
    public void SpriteAlphaCutoff_Parse_CapturesUsingParamsAndMixin()
    {
        var doc = new ShaderParser().ParseSdslDocument(ReadCombinedSource()).Document!;
        var effect = Assert.Single(doc.EffectBlocks, e => e.EffectName == "SpriteAlphaCutoffEffect");
        Assert.Contains("SpriteBaseKeys", effect.UsingParams);
        Assert.Contains(effect.Mixins, m => m.Contains("SpriteAlphaCutoff<SpriteBaseKeys.ColorIsSRgb>", StringComparison.Ordinal));
    }

    [Fact]
    public void SpriteAlphaCutoff_Parse_EmitsEffectUnsupportedDiagnostics()
    {
        var result = new ShaderParser().ParseSdslDocument(ReadCombinedSource());
        Assert.Contains(result.Diagnostics, d => d.Code == "SD400");
        Assert.Contains(result.Diagnostics, d => d.Code == "SD401");
        Assert.Contains(result.Diagnostics, d => d.Code == "SD402");
    }

    [Fact]
    public void SpriteAlphaCutoff_ShaderLowering_DoesNotRequireEffectLowering()
    {
        var doc = new ShaderParser().ParseSdslDocument(ReadCombinedSource()).Document!;
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(
            doc,
            "SpriteAlphaCutoff",
            new ShaderSpecialization(new Dictionary<string, bool> { ["TSRgb"] = false }));

        Assert.DoesNotContain("partial effect", lowered.Hlsl, StringComparison.Ordinal);
        Assert.NotEmpty(lowered.Hlsl);
    }
}
