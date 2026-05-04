using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;

namespace StriV.ShaderPipeline.Tests;

public class SpriteBatchPairTests
{
    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    private static string ReadCombinedSpritePair()
    {
        var spriteBase = ReadFixture("sdsl/sprite/SpriteBase.sdsl");
        var spriteBatch = ReadFixture("sdsl/sprite/SpriteBatchShader.sdsl");
        return spriteBase + "\n" + spriteBatch;
    }

    [Fact]
    public void SpriteBatchPair_Parse_ResolvesBaseShader()
    {
        var result = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        Assert.NotNull(result.Document);
        Assert.True(result.Document!.Shaders.Count >= 2);

        var spriteBatch = Assert.Single(result.Document.Shaders.Where(s => s.Name == "SpriteBatchShader"));
        Assert.Contains("SpriteBase", spriteBatch.BaseShaders);

        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(result.Document, "SpriteBatchShader");
        Assert.DoesNotContain(lowered.Diagnostics, d => d.Code == "SD310" && d.Message.Contains("SpriteBase", StringComparison.Ordinal));
    }

    [Fact]
    public void SpriteBatchPair_Parse_CapturesGenericParameter()
    {
        var result = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var spriteBatch = Assert.Single(result.Document!.Shaders.Where(s => s.Name == "SpriteBatchShader"));

        Assert.Equal("bool TSRgb", spriteBatch.GenericParametersText);
        Assert.Contains(result.Diagnostics, d => d.Code == "SD301");
    }

    [Fact]
    public void SpriteBatchPair_Merge_ProducesMergedStreamLayout()
    {
        var doc = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair()).Document!;
        var result = new ShaderLowerer().LowerSdslDocumentToHlsl(doc, "SpriteBatchShader");

        Assert.Contains("float4 Position : POSITION;", result.Hlsl);
        Assert.Contains("float4 Color : COLOR;", result.Hlsl);
        Assert.Contains("float4 ColorAdd : COLOR1;", result.Hlsl);
        Assert.Contains("float Swizzle : BATCH_SWIZZLE;", result.Hlsl);

        var iPosition = result.Hlsl.IndexOf("float4 Position : POSITION;", StringComparison.Ordinal);
        var iColor = result.Hlsl.IndexOf("float4 Color : COLOR;", StringComparison.Ordinal);
        var iColorAdd = result.Hlsl.IndexOf("float4 ColorAdd : COLOR1;", StringComparison.Ordinal);
        var iSwizzle = result.Hlsl.IndexOf("float Swizzle : BATCH_SWIZZLE;", StringComparison.Ordinal);
        Assert.True(iPosition < iColor && iColor < iColorAdd && iColorAdd < iSwizzle);
    }

    [Fact]
    public void SpriteBatchPair_Merge_RewritesResolvableBaseCalls()
    {
        var doc = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair()).Document!;
        var result = new ShaderLowerer().LowerSdslDocumentToHlsl(doc, "SpriteBatchShader");

        Assert.Contains("__base_SpriteBase_VSMain", result.Hlsl);
        Assert.Contains("__base_SpriteBase_Shading", result.Hlsl);
        Assert.DoesNotContain("base.VSMain()", result.Hlsl, StringComparison.Ordinal);
    }

    [Fact]
    public void SpriteBatchPair_Merge_DiagnosesOnlyTrulyUnsupportedSemantics()
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(parsed.Document!, "SpriteBatchShader");

        Assert.Contains(parsed.Diagnostics, d => d.Code == "SD301");
        Assert.DoesNotContain(lowered.Diagnostics, d => d.Code == "SD310" && d.Message.Contains("SpriteBase", StringComparison.Ordinal));
        Assert.DoesNotContain(lowered.Diagnostics, d => d.Code == "SD300");
    }
}
