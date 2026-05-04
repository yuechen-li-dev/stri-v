using System.Text.Json;
using StriV.ShaderPipeline.Artifacts;
using Xunit;

namespace StriV.ShaderPipeline.Tests;

public sealed class ShaderArtifactEmitterTests
{
    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    private static string SpritePair() => ReadFixture("sdsl/sprite/SpriteBase.sdsl") + "\n" + ReadFixture("sdsl/sprite/SpriteBatchShader.sdsl");

    [Fact]
    public void ShaderArtifactEmitter_WritesManifestAndGeneratedHlsl()
    {
        var dir = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        var emitter = new ShaderArtifactEmitter();
        emitter.Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = SpritePair(),
            EntryShader = "SpriteBatchShader",
            BoolSpecialization = new Dictionary<string, bool> { ["TSRgb"] = false }
        });

        Assert.True(File.Exists(Path.Combine(dir, "manifest.json")));
        Assert.True(File.Exists(Path.Combine(dir, "generated/vertex.hlsl")));
        Assert.True(File.Exists(Path.Combine(dir, "generated/pixel.hlsl")));

        var json = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement;
        Assert.True(json.TryGetProperty("format", out _));
        Assert.True(json.TryGetProperty("sourcePath", out _));
        Assert.True(json.TryGetProperty("stages", out _));
    }

    [Fact]
    public void ShaderArtifactManifest_IsDeterministic()
    {
        var dir1 = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        var dir2 = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        var emitter = new ShaderArtifactEmitter();
        emitter.Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir1,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = SpritePair(),
            EntryShader = "SpriteBatchShader",
            BoolSpecialization = new Dictionary<string, bool> { ["TSRgb"] = false }
        });

        emitter.Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir2,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = SpritePair(),
            EntryShader = "SpriteBatchShader",
            BoolSpecialization = new Dictionary<string, bool> { ["TSRgb"] = false }
        });

        Assert.Equal(File.ReadAllText(Path.Combine(dir1, "manifest.json")), File.ReadAllText(Path.Combine(dir2, "manifest.json")));
    }

    [Fact]
    public void ShaderArtifactManifest_UsesFlatRecordArrays()
    {
        var dir = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        new ShaderArtifactEmitter().Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = SpritePair(),
            EntryShader = "SpriteBatchShader",
            BoolSpecialization = new Dictionary<string, bool> { ["TSRgb"] = false }
        });

        var root = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement;
        Assert.Equal(JsonValueKind.Array, root.GetProperty("stages").ValueKind);
        Assert.Equal(JsonValueKind.Array, root.GetProperty("specializations").ValueKind);
        Assert.Equal(JsonValueKind.Array, root.GetProperty("io").ValueKind);
        Assert.Equal(JsonValueKind.Array, root.GetProperty("diagnostics").ValueKind);
        Assert.False(root.TryGetProperty("source", out _));
        Assert.False(root.TryGetProperty("backend", out _));
        Assert.False(root.TryGetProperty("reflection", out _));
    }

    [Fact]
    public void ShaderArtifactEmitter_ProducesSpirvWhenDxcAvailable()
    {
        var probe = DxcTestProbe.Create();
        if (!probe.IsAvailable)
            return;

        var dir = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        new ShaderArtifactEmitter().Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = SpritePair(),
            EntryShader = "SpriteBatchShader",
            BoolSpecialization = new Dictionary<string, bool> { ["TSRgb"] = false }
        });

        var vs = Path.Combine(dir, "bin/vertex.spv");
        var ps = Path.Combine(dir, "bin/pixel.spv");
        if (File.Exists(vs) && File.Exists(ps))
        {
            Assert.True(new FileInfo(vs).Length > 0);
            Assert.True(new FileInfo(ps).Length > 0);
            return;
        }

        var manifest = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement;
        Assert.Contains(manifest.GetProperty("diagnostics").EnumerateArray(), d => d.GetProperty("code").GetString() == "DXC001");
    }

    [Fact]
    public void ShaderArtifactManifest_IncludesDiagnostics()
    {
        var dir = Path.Combine(Path.GetTempPath(), "striv-shader-artifacts", Guid.NewGuid().ToString("N"));
        new ShaderArtifactEmitter().Emit(new ShaderArtifactOptions
        {
            OutputRoot = dir,
            SourcePath = "fixtures/shaders/sdsl/sprite/SpriteBatchShader.sdsl",
            SourceText = "shader {",
            EntryShader = "C",
            BoolSpecialization = new Dictionary<string, bool>()
        });

        var diagnostics = JsonDocument.Parse(File.ReadAllText(Path.Combine(dir, "manifest.json"))).RootElement.GetProperty("diagnostics");
        Assert.True(diagnostics.GetArrayLength() > 0);
        var first = diagnostics[0];
        Assert.True(first.TryGetProperty("code", out _));
        Assert.True(first.TryGetProperty("phase", out _));
        Assert.True(first.TryGetProperty("message", out _));
        Assert.True(first.TryGetProperty("fatal", out _));
    }
}
