using System.Text.RegularExpressions;
using StriV.ShaderPipeline.Lowering;
using StriV.ShaderPipeline.Parsing;
using Xunit;
using Xunit.Abstractions;

namespace StriV.ShaderPipeline.Tests;

public class SpriteBatchDxcInventoryTests
{
    private readonly ITestOutputHelper output;

    public SpriteBatchDxcInventoryTests(ITestOutputHelper output)
    {
        this.output = output;
    }

    private static string ReadFixture(string rel)
    {
        var path = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "fixtures/shaders", rel));
        return File.ReadAllText(path);
    }

    private static string ReadCombinedSpritePair()
        => ReadFixture("sdsl/sprite/SpriteBase.sdsl") + "\n" + ReadFixture("sdsl/sprite/SpriteBatchShader.sdsl");

    private static LoweringResult LowerSpecialized(bool tsRgb)
    {
        var parsed = new ShaderParser().ParseSdslDocument(ReadCombinedSpritePair());
        return new ShaderLowerer().LowerSdslDocumentToHlsl(
            parsed.Document!,
            "SpriteBatchShader",
            new ShaderSpecialization(new Dictionary<string, bool> { ["TSRgb"] = tsRgb }));
    }

    [Fact]
    public void SpriteBatchSpecializedFalse_LoweredOutput_HasNoStandaloneTSRgb()
    {
        var lowered = LowerSpecialized(tsRgb: false);
        Assert.DoesNotMatch(new Regex(@"\bTSRgb\b"), lowered.Hlsl);
    }

    [Fact]
    public void SpriteBatchSpecializedFalse_LoweredOutput_HasNoRawShaderStageKeywords()
    {
        var lowered = LowerSpecialized(tsRgb: false);
        Assert.DoesNotContain("shader ", lowered.Hlsl, StringComparison.Ordinal);
        Assert.DoesNotContain("stage stream", lowered.Hlsl, StringComparison.Ordinal);
        Assert.DoesNotContain("stage override", lowered.Hlsl, StringComparison.Ordinal);
        Assert.DoesNotContain("base.VSMain()", lowered.Hlsl, StringComparison.Ordinal);
        Assert.Contains("__base_SpriteBase_VSMain", lowered.Hlsl, StringComparison.Ordinal);
    }

    [Fact]
    public void SpriteBatchSpecializedFalse_LoweredOutput_HasExpectedRemainingDiagnostics()
    {
        var lowered = LowerSpecialized(tsRgb: false);
        Assert.DoesNotContain(lowered.Diagnostics, d => d.Code is "SD300" or "SD301" or "SD310" or "SD320");
        Assert.All(lowered.Diagnostics, d => Assert.Contains(d.Code, new[] { "SD312", "SD330", "SD331", "SD340" }));
    }

    [Fact]
    public void SpriteBatchSpecializedFalse_DxcInventory_WhenAvailable()
    {
        var lowered = LowerSpecialized(tsRgb: false);
        var dxc = DxcTestProbe.Create();
        if (!dxc.IsAvailable)
        {
            output.WriteLine($"DXC inventory skipped: {dxc.UnavailableReason}");
            return;
        }

        var tempDir = Path.Combine(Path.GetTempPath(), "striv-shader-pipeline", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);
        var hlslPath = Path.Combine(tempDir, "spritebatch.specialized.false.lowered.hlsl");
        File.WriteAllText(hlslPath, lowered.Hlsl);
        output.WriteLine($"Wrote lowered HLSL snapshot to: {hlslPath}");

        var ext = dxc.SupportsSpirv ? "spv" : "dxil";
        var extraArgs = dxc.SupportsSpirv ? "-spirv" : string.Empty;
        var vsPath = Path.Combine(tempDir, $"spritebatch.specialized.false.vs.{ext}");
        var psPath = Path.Combine(tempDir, $"spritebatch.specialized.false.ps.{ext}");

        var vsResult = dxc.Compile(hlslPath, "vs_6_0", "VSMain", vsPath, extraArgs);
        var psResult = dxc.Compile(hlslPath, "ps_6_0", "PSMain", psPath, extraArgs);

        output.WriteLine($"VS exit: {vsResult.ExitCode}");
        output.WriteLine($"PS exit: {psResult.ExitCode}");
        output.WriteLine($"VS stderr: {vsResult.StdErr}");
        output.WriteLine($"PS stderr: {psResult.StdErr}");

        if (vsResult.ExitCode == 0 && psResult.ExitCode == 0)
        {
            Assert.True(File.Exists(vsPath) && new FileInfo(vsPath).Length > 0);
            Assert.True(File.Exists(psPath) && new FileInfo(psPath).Length > 0);
            return;
        }

        var mergedErrors = string.Concat(vsResult.StdOut, "\n", vsResult.StdErr, "\n", psResult.StdOut, "\n", psResult.StdErr);
        Assert.True(
            mergedErrors.Contains("no matching function for call", StringComparison.OrdinalIgnoreCase)
            || mergedErrors.Contains("undeclared identifier", StringComparison.OrdinalIgnoreCase)
            || mergedErrors.Contains("expected", StringComparison.OrdinalIgnoreCase),
            $"Unexpected DXC failure shape.\n{mergedErrors}");
    }
}
