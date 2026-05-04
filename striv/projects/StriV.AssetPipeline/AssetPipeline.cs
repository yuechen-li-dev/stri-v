using StriV.ShaderPipeline.Artifacts;
using StriV.ShaderPipeline.Parsing;
using Tomlyn;
using Tomlyn.Model;

namespace StriV.AssetPipeline;

public sealed class AssetManifest
{
    public List<ShaderAssetRecord> Shaders { get; } = [];
    public List<ShaderSpecializationRecord> Specializations { get; } = [];
    public List<ShaderEffectRecord> Effects { get; } = [];
}

public sealed record ShaderAssetRecord(string Id, string Source, string Entry, string Backend, string Profile);
public sealed record ShaderSpecializationRecord(string Shader, string Name, string Type, object? Value);
public sealed record ShaderEffectRecord(string Shader, string Name, string? Namespace);

public sealed record AssetDiagnostic(string Code, string Severity, string Message, string SourcePath, int Line = 1, int Column = 1, bool Fatal = true);
public sealed record ShaderAssetBuildResult(string ShaderId, string OutputDirectory, string ManifestPath, bool Success);
public sealed record AssetPipelineResult(IReadOnlyList<AssetDiagnostic> Diagnostics, IReadOnlyList<ShaderAssetBuildResult> Built, IReadOnlyList<string> FailedShaderIds);

public static class AssetManifestParser
{
    public static (AssetManifest? Manifest, IReadOnlyList<AssetDiagnostic> Diagnostics) Parse(string tomlText, string manifestPath)
    {
        if (!Toml.TryToModel<TomlTable>(tomlText, out var table, out var diagnostics) || table is null)
        {
            return (null, [new AssetDiagnostic("AM100", "error", diagnostics?.ToString() ?? "TOML parse failure.", manifestPath)]);
        }

        var manifest = new AssetManifest();
        if (table.TryGetValue("shader", out var shaderObj) && shaderObj is TomlTableArray shaderTables)
        {
            foreach (var item in shaderTables.OfType<TomlTable>())
            {
                manifest.Shaders.Add(new ShaderAssetRecord(
                    item.TryGetValue("id", out var id) ? id?.ToString() ?? string.Empty : string.Empty,
                    item.TryGetValue("source", out var source) ? source?.ToString() ?? string.Empty : string.Empty,
                    item.TryGetValue("entry", out var entry) ? entry?.ToString() ?? string.Empty : string.Empty,
                    item.TryGetValue("backend", out var backend) ? backend?.ToString() ?? string.Empty : string.Empty,
                    item.TryGetValue("profile", out var profile) ? profile?.ToString() ?? string.Empty : string.Empty));
            }

            foreach (var item in shaderTables.OfType<TomlTable>().Where(x => x.TryGetValue("specialization", out _)))
            {
                if (item["specialization"] is TomlTableArray specializationArray)
                foreach (var s in specializationArray.OfType<TomlTable>())
                    manifest.Specializations.Add(new ShaderSpecializationRecord(s["shader"]?.ToString() ?? string.Empty, s["name"]?.ToString() ?? string.Empty, s["type"]?.ToString() ?? string.Empty, s.TryGetValue("value", out var v) ? v : null));
            }

            foreach (var item in shaderTables.OfType<TomlTable>().Where(x => x.TryGetValue("effect", out _)))
            {
                if (item["effect"] is TomlTableArray effectArray)
                foreach (var e in effectArray.OfType<TomlTable>())
                    manifest.Effects.Add(new ShaderEffectRecord(e["shader"]?.ToString() ?? string.Empty, e["name"]?.ToString() ?? string.Empty, e.TryGetValue("namespace", out var n) ? n?.ToString() : null));
            }
        }

        return (manifest, []);
    }
}

public static class AssetManifestValidator
{
    public static IReadOnlyList<AssetDiagnostic> Validate(AssetManifest manifest, string manifestPath)
    {
        var diags = new List<AssetDiagnostic>();
        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (var shader in manifest.Shaders)
        {
            if (!ids.Add(shader.Id)) diags.Add(new("AM200", "error", $"Duplicate asset ID '{shader.Id}'.", manifestPath));
            if (new[] { shader.Id, shader.Source, shader.Entry, shader.Backend, shader.Profile }.Any(string.IsNullOrWhiteSpace)) diags.Add(new("AM201", "error", $"Missing required field in shader '{shader.Id}'.", manifestPath));
            if (Path.IsPathRooted(shader.Source) || shader.Source.Contains("..") || shader.Source.Contains('\\')) diags.Add(new("AM202", "error", $"Invalid source path '{shader.Source}'.", manifestPath));
            if (!string.Equals(shader.Backend, "vulkan", StringComparison.Ordinal) || !string.Equals(shader.Profile, "default", StringComparison.Ordinal)) diags.Add(new("AM203", "error", $"Unsupported backend/profile '{shader.Backend}/{shader.Profile}'.", manifestPath));
            if (!System.Text.RegularExpressions.Regex.IsMatch(shader.Id ?? string.Empty, "^[a-z0-9._-]+$")) diags.Add(new("AM201", "error", $"Invalid id '{shader.Id}'.", manifestPath));
        }

        var shaderIds = manifest.Shaders.Select(s => s.Id).ToHashSet(StringComparer.Ordinal);
        foreach (var s in manifest.Specializations)
        {
            if (!shaderIds.Contains(s.Shader)) diags.Add(new("AM204", "error", $"Unknown shader reference '{s.Shader}'.", manifestPath));
            if (!string.Equals(s.Type, "bool", StringComparison.Ordinal) || s.Value is not bool) diags.Add(new("AM207", "error", $"Specialization '{s.Name}' must be bool type with bool value.", manifestPath));
        }

        foreach (var e in manifest.Effects.Where(e => !shaderIds.Contains(e.Shader))) diags.Add(new("AM204", "error", $"Unknown shader reference '{e.Shader}'.", manifestPath));

        foreach (var dup in manifest.Specializations.GroupBy(x => (x.Shader, x.Name)).Where(g => g.Count() > 1)) diags.Add(new("AM205", "error", $"Duplicate specialization '{dup.Key.Name}' for shader '{dup.Key.Shader}'.", manifestPath));

        return diags;
    }
}

public sealed class AssetPipelineRunner
{
    public AssetPipelineResult BuildShaders(AssetManifest manifest, string manifestPath, string outputRoot, bool strictDxc = false)
    {
        var diags = AssetManifestValidator.Validate(manifest, manifestPath).ToList();
        var built = new List<ShaderAssetBuildResult>();
        var failed = new List<string>();
        if (diags.Any(d => d.Fatal)) return new(diags, built, manifest.Shaders.Select(s => s.Id).ToList());

        var manifestDir = Path.GetDirectoryName(Path.GetFullPath(manifestPath))!;
        var parser = new ShaderParser();
        foreach (var shader in manifest.Shaders)
        {
            var srcPath = Path.GetFullPath(Path.Combine(manifestDir, shader.Source));
            var source = File.ReadAllText(srcPath);
            var parsed = parser.ParseSdslDocument(source);
            var shaderDoc = parsed.Document?.Shaders.FirstOrDefault(s => s.Name == shader.Entry);
            var specs = manifest.Specializations.Where(s => s.Shader == shader.Id).ToList();
            if (shaderDoc is not null)
            {
                var genericNames = shaderDoc.GenericParameters.Select(p => p.Name).ToHashSet(StringComparer.Ordinal);
                foreach (var spec in specs.Where(x => !genericNames.Contains(x.Name))) diags.Add(new("AM206", "error", $"Unknown specialization parameter '{spec.Name}' for shader '{shader.Entry}'.", manifestPath));
            }

            var shaderOut = Path.Combine(outputRoot, "shaders", shader.Id);
            var emitter = new ShaderArtifactEmitter();
            var artifact = emitter.Emit(new ShaderArtifactOptions
            {
                SourcePath = srcPath,
                SourceText = source,
                EntryShader = shader.Entry,
                OutputRoot = shaderOut,
                BoolSpecialization = specs.ToDictionary(x => x.Name, x => (bool)x.Value!, StringComparer.Ordinal),
                StrictDxc = strictDxc
            });
            var manifestJsonPath = Path.Combine(shaderOut, "manifest.json");
            File.WriteAllText(manifestJsonPath, ShaderArtifactJsonWriter.ToJson(artifact));
            built.Add(new(shader.Id, shaderOut, manifestJsonPath, true));
        }

        return new(diags, built, failed);
    }
}
