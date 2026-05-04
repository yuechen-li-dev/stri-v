using System.Diagnostics;
using StriV.ShaderPipeline.Lexing;
using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace StriV.ShaderPipeline.Tests;

public class ShaderPipelineTests
{
    private readonly ITestOutputHelper output;

    public ShaderPipelineTests(ITestOutputHelper output)
    {
        this.output = output;
    }

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
        Assert.Null(result.Document.GenericParametersText);
        Assert.Empty(result.Document.BaseShaders);
        Assert.Collection(result.Document.Streams,
            s => { Assert.Equal("Position", s.Name); Assert.Equal("float4", s.Type); Assert.Equal("SV_Position", s.Semantic); },
            s => { Assert.Equal("Color", s.Name); Assert.Equal("float4", s.Type); Assert.Equal("COLOR0", s.Semantic); });
    }

    [Fact]
    public void SpriteBatchShader_Parse_RecognizesShaderHeader()
    {
        var source = ReadFixture("sdsl/SpriteBatchShader.sdsl");
        var result = new ShaderParser().ParseSdsl(source);
        Assert.NotNull(result.Document);
        Assert.Equal("SpriteBatchShader", result.Document!.Name);
        Assert.Equal("bool TSRgb", result.Document.GenericParametersText);
        Assert.Contains("SpriteBase", result.Document.BaseShaders);
    }

    [Fact]
    public void SpriteBatchShader_Parse_CapturesStreamsAndStageMethods()
    {
        var shader = new ShaderParser().ParseSdsl(ReadFixture("sdsl/SpriteBatchShader.sdsl")).Document!;
        Assert.Equal(3, shader.Streams.Count);
        Assert.Equal(2, shader.Methods.Count);
        Assert.Contains(shader.Methods, m => m.Body.Contains("base.", StringComparison.Ordinal));
        Assert.Contains(shader.Methods, m => m.Body.Contains("streams.", StringComparison.Ordinal));
    }

    [Fact]
    public void SpriteBatchShader_Parse_DetectsUnsupportedGenericAndInheritanceSemantics()
    {
        var result = new ShaderParser().ParseSdsl(ReadFixture("sdsl/SpriteBatchShader.sdsl"));
        Assert.Contains(result.Diagnostics, d => d.Code == "SD300");
        Assert.Contains(result.Diagnostics, d => d.Code == "SD301");
    }

    [Fact]
    public void SpriteBatchShader_Lowering_EmitsDeterministicDiagnosticsForBaseCalls()
    {
        var shader = new ShaderParser().ParseSdsl(ReadFixture("sdsl/SpriteBatchShader.sdsl")).Document!;
        var result = new ShaderLowerer().LowerSdslToHlsl(shader);
        Assert.Contains(result.Diagnostics, d => d.Code == "SD302");
        Assert.Contains("TODO(SD301)", result.Hlsl);
        Assert.Contains("TODO(SD300)", result.Hlsl);
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
    public void LoweredSimpleStreamShader_HasNoSdslKeywords()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var shader = new ShaderParser().ParseSdsl(source).Document!;
        var lowered = new ShaderLowerer().LowerSdslToHlsl(shader).Hlsl;

        Assert.DoesNotContain("shader ", lowered, StringComparison.Ordinal);
        Assert.DoesNotContain("stage ", lowered, StringComparison.Ordinal);
        Assert.DoesNotContain("stream ", lowered, StringComparison.Ordinal);
        Assert.DoesNotContain("override ", lowered, StringComparison.Ordinal);
        Assert.DoesNotContain("__streams", lowered, StringComparison.Ordinal);
    }

    [Fact]
    public void LoweredSimpleStreamShader_CanCompileWithDxc_WhenAvailable()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var shader = new ShaderParser().ParseSdsl(source).Document!;
        var lowered = new ShaderLowerer().LowerSdslToHlsl(shader).Hlsl;

        var dxc = DxcTestProbe.Create();
        if (!dxc.IsAvailable)
        {
            output.WriteLine($"DXC compile-smoke skipped: {dxc.UnavailableReason}");
            return;
        }

        output.WriteLine($"Using dxc at: {dxc.ExecutablePath}");
        var tempDir = Path.Combine(Path.GetTempPath(), "striv-shader-pipeline", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var hlslPath = Path.Combine(tempDir, "simple_stream_shader.lowered.hlsl");
        File.WriteAllText(hlslPath, lowered);

        var ext = dxc.SupportsSpirv ? "spv" : "dxil";
        var extraArgs = dxc.SupportsSpirv ? "-spirv" : string.Empty;
        var vsPath = Path.Combine(tempDir, $"simple_stream_shader.vs.{ext}");
        var psPath = Path.Combine(tempDir, $"simple_stream_shader.ps.{ext}");

        var vsResult = dxc.Compile(hlslPath, "vs_6_0", "VSMain", vsPath, extraArgs);
        var psResult = dxc.Compile(hlslPath, "ps_6_0", "PSMain", psPath, extraArgs);

        Assert.True(vsResult.ExitCode == 0, $"VS compile failed.\nstdout:\n{vsResult.StdOut}\nstderr:\n{vsResult.StdErr}");
        Assert.True(psResult.ExitCode == 0, $"PS compile failed.\nstdout:\n{psResult.StdOut}\nstderr:\n{psResult.StdErr}");
        Assert.True(File.Exists(vsPath) && new FileInfo(vsPath).Length > 0, $"Missing or empty output: {vsPath}");
        Assert.True(File.Exists(psPath) && new FileInfo(psPath).Length > 0, $"Missing or empty output: {psPath}");
    }

    [Fact]
    public void PlainHlslFixture_CanCompileWithDxc_WhenAvailable()
    {
        var source = ReadFixture("plain/simple_vertex_pixel.hlsl");
        var dxc = DxcTestProbe.Create();
        if (!dxc.IsAvailable)
        {
            output.WriteLine($"DXC plain HLSL compile-smoke skipped: {dxc.UnavailableReason}");
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "striv-shader-pipeline", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var hlslPath = Path.Combine(tempDir, "simple_vertex_pixel.hlsl");
        File.WriteAllText(hlslPath, source);

        var ext = dxc.SupportsSpirv ? "spv" : "dxil";
        var extraArgs = dxc.SupportsSpirv ? "-spirv" : string.Empty;
        var vsPath = Path.Combine(tempDir, $"simple_vertex_pixel.vs.{ext}");
        var psPath = Path.Combine(tempDir, $"simple_vertex_pixel.ps.{ext}");

        var vsResult = dxc.Compile(hlslPath, "vs_6_0", "VSMain", vsPath, extraArgs);
        var psResult = dxc.Compile(hlslPath, "ps_6_0", "PSMain", psPath, extraArgs);

        Assert.True(vsResult.ExitCode == 0, $"VS compile failed.\nstdout:\n{vsResult.StdOut}\nstderr:\n{vsResult.StdErr}");
        Assert.True(psResult.ExitCode == 0, $"PS compile failed.\nstdout:\n{psResult.StdOut}\nstderr:\n{psResult.StdErr}");
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
