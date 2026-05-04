using StriV.ShaderPipeline.Lexing;
using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;

namespace StriV.ShaderPipeline.Tests;

public class ShaderPipelineTests
{
    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    [Fact]
    public void Parse_Sdsl_Ast_Contains_Expected_Shape()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var result = new ShaderParser().ParseSdsl(source);
        Assert.True(result.Success);
        Assert.Equal("SimpleStreamShader", result.Document!.Name);
        Assert.Collection(result.Document.Streams,
            s => { Assert.Equal("Position", s.Name); Assert.Equal("float4", s.Type); Assert.Equal("SV_Position", s.Semantic); },
            s => { Assert.Equal("Color", s.Name); Assert.Equal("float4", s.Type); Assert.Equal("COLOR0", s.Semantic); });
    }

    [Fact]
    public void Lowering_Emits_Deterministic_Stage_Stream_Model()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var shader = new ShaderParser().ParseSdsl(source).Document!;
        var lowered = new ShaderLowerer().LowerSdslToHlsl(shader).Hlsl;
        Assert.Contains("struct StriVStageStreams", lowered);
        Assert.Contains("float4 Position : SV_Position;", lowered);
        Assert.Contains("StriVStageStreams VSMain()", lowered);
        Assert.Contains("StriVStageStreams streams;", lowered);
        Assert.Contains("return streams;", lowered);
        Assert.Contains("float4 PSMain(StriVStageStreams streams) : SV_Target", lowered);
        Assert.DoesNotContain("static StageStreams __streams", lowered);
        Assert.DoesNotContain("__streams.", lowered);
    }

    [Fact]
    public void Lowering_Diagnoses_Duplicate_Stream_Name_And_Semantic()
    {
        const string source = """
shader D {
 stage stream float4 Position : SV_Position;
 stage stream float4 Position : COLOR0;
 stage stream float4 Color : COLOR0;
 stage override void VSMain(){ streams.Position = 0; }
 stage override float4 PSMain(){ return streams.Color; }
}
""";
        var shader = new ShaderParser().ParseSdsl(source).Document!;
        var result = new ShaderLowerer().LowerSdslToHlsl(shader);
        Assert.Contains(result.Diagnostics, d => d.Code == "SD200");
        Assert.Contains(result.Diagnostics, d => d.Code == "SD201");
    }

    [Fact]
    public void Parse_Plain_Hlsl_NoDiagnostics_And_Emit()
    {
        var source = ReadFixture("plain/simple_vertex_pixel.hlsl");
        var parser = new ShaderParser();
        var result = parser.ParseHlsl(source);
        Assert.True(result.Success);
        var emitted = new ShaderLowerer().EmitHlsl(result.Document!);
        Assert.Contains("float4 PSMain", emitted);
    }
}
