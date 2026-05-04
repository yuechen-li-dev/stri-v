using Xunit;
using StriV.AssetPipeline;

namespace StriV.AssetPipeline.Tests;

public class AssetPipelineTests
{
    private static string FixtureRoot => Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures", "assets", "shader_manifest"));

    [Fact]
    public void Parse_Success()
    {
        var manifestPath = Path.Combine(FixtureRoot, "assets.toml");
        var (manifest, diags) = AssetManifestParser.Parse(File.ReadAllText(manifestPath), manifestPath);
        Assert.Empty(diags);
        Assert.NotNull(manifest);
        Assert.Single(manifest!.Shaders);
        Assert.Single(manifest.Specializations);
    }

    [Fact]
    public void Validate_Success()
    {
        var manifestPath = Path.Combine(FixtureRoot, "assets.toml");
        var (manifest, _) = AssetManifestParser.Parse(File.ReadAllText(manifestPath), manifestPath);
        var diags = AssetManifestValidator.Validate(manifest!, manifestPath);
        Assert.DoesNotContain(diags, d => d.Code.StartsWith("AM", StringComparison.Ordinal));
    }

    [Fact]
    public void Build_Success_Or_DxcWarning()
    {
        var manifestPath = Path.Combine(FixtureRoot, "assets.toml");
        var (manifest, _) = AssetManifestParser.Parse(File.ReadAllText(manifestPath), manifestPath);
        var output = Path.Combine(Path.GetTempPath(), "striv-asset-tests", Guid.NewGuid().ToString("N"));
        var result = new AssetPipelineRunner().BuildShaders(manifest!, manifestPath, output, strictDxc: false);
        var shaderDir = Path.Combine(output, "shaders", "shader.sprite_batch");
        Assert.True(Directory.Exists(shaderDir));
        Assert.True(File.Exists(Path.Combine(shaderDir, "manifest.json")));
        Assert.NotEmpty(Directory.GetFiles(Path.Combine(shaderDir, "generated"), "*.hlsl", SearchOption.AllDirectories));
    }

    [Fact] public void DuplicateId_AM200() => AssertCode("[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='vulkan'\nprofile='default'\n[[shader]]\nid='a'\nsource='s2'\nentry='e'\nbackend='vulkan'\nprofile='default'", "AM200");
    [Fact] public void MissingRequired_AM201() => AssertCode("[[shader]]\nid='a'\nsource='s'\nbackend='vulkan'\nprofile='default'", "AM201");
    [Fact] public void InvalidPath_AM202() => AssertCode("[[shader]]\nid='a'\nsource='../escape.sdsl'\nentry='e'\nbackend='vulkan'\nprofile='default'", "AM202");
    [Fact] public void Unsupported_AM203() => AssertCode("[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='metal'\nprofile='default'", "AM203");
    [Fact] public void UnknownRef_AM204() => AssertCode("[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='vulkan'\nprofile='default'\n[[shader.specialization]]\nshader='missing'\nname='X'\ntype='bool'\nvalue=false", "AM204");
    [Fact] public void DuplicateSpec_AM205() => AssertCode("[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='vulkan'\nprofile='default'\n[[shader.specialization]]\nshader='a'\nname='X'\ntype='bool'\nvalue=false\n[[shader.specialization]]\nshader='a'\nname='X'\ntype='bool'\nvalue=true", "AM205");
    [Fact] public void TypeMismatch_AM207() => AssertCode("[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='vulkan'\nprofile='default'\n[[shader.specialization]]\nshader='a'\nname='X'\ntype='bool'\nvalue='false'", "AM207");

    [Fact]
    public void EffectRecord_Parses_And_UnknownRef_AM204()
    {
        var toml = "[[shader]]\nid='a'\nsource='s'\nentry='e'\nbackend='vulkan'\nprofile='default'\n[[shader.effect]]\nshader='missing'\nname='Fx'\nnamespace='Stride.Rendering'";
        var (manifest, _) = AssetManifestParser.Parse(toml, "m");
        Assert.Single(manifest!.Effects);
        Assert.Contains(AssetManifestValidator.Validate(manifest, "m"), d => d.Code == "AM204");
    }

    private static void AssertCode(string toml, string code)
    {
        var (manifest, parseDiags) = AssetManifestParser.Parse(toml, "mem.toml");
        Assert.Empty(parseDiags);
        Assert.Contains(AssetManifestValidator.Validate(manifest!, "mem.toml"), d => d.Code == code);
    }
}
