using Xunit;
using StriV.ShaderPipeline.Parsing;
using StriV.ShaderPipeline.Ast;
using StriV.ShaderPipeline.Lexing;
using StriV.ShaderPipeline.Lowering;

namespace StriV.ShaderPipeline.Tests;

public class StreamUsageAnalyzerTests
{
    [Fact]
    public void StreamUsage_DetectsVsWrite()
    {
        var result = AnalyzeMethod("VSMain", "streams.Position = float4(0,0,0,1);");
        Assert.Contains(result.Accesses, a => a.StreamName == "Position" && a.Stage == StageKind.Vertex && a.Kind == StreamAccessKind.Write);
    }

    [Fact]
    public void StreamUsage_DetectsVsRead()
    {
        var result = AnalyzeMethod("VSMain", "float4 p = streams.Position;");
        Assert.Contains(result.Accesses, a => a.StreamName == "Position" && a.Stage == StageKind.Vertex && a.Kind == StreamAccessKind.Read);
    }

    [Fact]
    public void StreamUsage_DetectsCompoundReadWrite()
    {
        var result = AnalyzeMethod("VSMain", "streams.Color *= 0.5;");
        Assert.Contains(result.Accesses, a => a.StreamName == "Color" && a.Kind == StreamAccessKind.ReadWrite);
    }

    [Fact]
    public void StreamUsage_DetectsSwizzleWrite()
    {
        var result = AnalyzeMethod("VSMain", "streams.Color.rgb = value;");
        Assert.Contains(result.Accesses, a => a.StreamName == "Color" && a.Kind == StreamAccessKind.Write);
    }

    [Fact]
    public void StreamUsage_IgnoresCommentsAndStrings()
    {
        var result = AnalyzeMethod("VSMain", "// streams.Fake = 1;\nvar s = \"streams.Fake\";\nstreams.Color = 1;");
        Assert.Single(result.Accesses);
        Assert.Equal("Color", result.Accesses[0].StreamName);
    }

    [Fact]
    public void SpriteBatch_UsageAnalysis_CapturesColorOrSwizzleUsage()
    {
        var src = """
shader SpriteBatchShader {
    stream float4 Position : SV_Position;
    stream float4 Color : COLOR0;
    stage override void VSMain(){ streams.Position = float4(0,0,0,1); }
    stage override float4 PSMain(){ return streams.Color; }
}
""";
        var parser = new ShaderParser();
        var doc = parser.ParseSdslDocument(src).Document!;
        var lowered = new ShaderLowerer().LowerSdslDocumentToHlsl(doc, "SpriteBatchShader");

        Assert.Contains(lowered.StreamUsage.Accesses, a => a.StreamName == "Color");
    }

    [Fact]
    public void StreamUsage_DiagnosesUndeclaredStream()
    {
        var result = AnalyzeMethod("VSMain", "streams.Missing = 1;", ["Position"]);
        Assert.Contains(result.Diagnostics, d => d.Code == "SD340");
    }

    private static StreamUsageAnalysisResult AnalyzeMethod(string name, string body, IReadOnlyCollection<string>? declared = null)
    {
        var method = new SdslStageMethod("void", name, "", body, [], [], new SourceSpan(0, body.Length, 1, 1));
        return new StreamUsageAnalyzer().Analyze([method], declared);
    }
}
