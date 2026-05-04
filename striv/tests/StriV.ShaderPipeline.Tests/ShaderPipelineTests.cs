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
    public void Lexer_Tokenizes_Hlsl_And_Sdsl()
    {
        var lexer = new ShaderLexer();
        var tokens = lexer.Lex("shader A { stage stream float4 Position : SV_Position; }\nfloat4 PSMain(){return 1.0;}" );
        Assert.Contains(tokens, t => t.Text == "shader" && t.Kind == TokenKind.Keyword);
        Assert.Contains(tokens, t => t.Text == "streams" || t.Text == "stream");
    }

    [Fact]
    public void Lexer_Preserves_SourceSpan()
    {
        var token = new ShaderLexer().Lex("float4 Color;")[0];
        Assert.Equal(1, token.Span.Line);
        Assert.Equal(1, token.Span.Column);
    }

    [Fact]
    public void Parse_Plain_Hlsl_NoDiagnostics_And_Emit()
    {
        var source = ReadFixture("plain/simple_vertex_pixel.hlsl");
        var parser = new ShaderParser();
        var result = parser.ParseHlsl(source);
        Assert.True(result.Success);
        Assert.Contains(result.Document!.Functions, f => f.Signature.Contains("VSMain"));
        var emitted = new ShaderLowerer().EmitHlsl(result.Document!);
        Assert.Contains("float4 PSMain", emitted);
    }

    [Fact]
    public void Parse_Sdsl_Ast_Contains_Expected_Shape()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var result = new ShaderParser().ParseSdsl(source);
        Assert.True(result.Success);
        Assert.Equal("SimpleStreamShader", result.Document!.Name);
        Assert.Equal(2, result.Document.Streams.Count);
        Assert.Equal(2, result.Document.Methods.Count);
        Assert.Contains("streams.Position", result.Document.Methods[0].Body);
    }

    [Fact]
    public void Lowering_Rewrites_Streams_And_Emits_Carrier()
    {
        var source = ReadFixture("sdsl/simple_stream_shader.sdsl");
        var shader = new ShaderParser().ParseSdsl(source).Document!;
        var lowered = new ShaderLowerer().LowerSdslToHlsl(shader);
        Assert.Contains("struct StageStreams", lowered);
        Assert.Contains("__streams.Position", lowered);
        Assert.DoesNotContain("shader SimpleStreamShader", lowered);
    }
}
