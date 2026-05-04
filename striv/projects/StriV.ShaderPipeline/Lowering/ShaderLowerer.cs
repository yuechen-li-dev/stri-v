using System.Text;
using StriV.ShaderPipeline.Ast;
using StriV.ShaderPipeline.Diagnostics;

namespace StriV.ShaderPipeline.Lowering;

public sealed record StreamBinding(string Type, string Name, string Semantic, int Line, int Column);
public sealed record StreamLayout(IReadOnlyList<StreamBinding> Bindings, IReadOnlyList<Diagnostic> Diagnostics);
public sealed record LoweringResult(string Hlsl, IReadOnlyList<Diagnostic> Diagnostics);

public sealed class ShaderLowerer
{
    public string EmitHlsl(HlslDocument document) => document.Source;

    public LoweringResult LowerSdslToHlsl(SdslShader shader) => LowerSdslDocumentToHlsl(new SdslDocument([shader], []), shader.Name);

    public LoweringResult LowerSdslDocumentToHlsl(SdslDocument doc, string entryShaderName)
    {
        var diags = new List<Diagnostic>(doc.Diagnostics);
        var registry = new Dictionary<string, SdslShader>(StringComparer.Ordinal);
        foreach (var s in doc.Shaders)
        {
            if (!registry.TryAdd(s.Name, s)) diags.Add(Diagnostic.Create("SD316", $"Duplicate shader name '{s.Name}'."));
        }
        if (!registry.TryGetValue(entryShaderName, out var shader)) return new("", diags);

        SdslShader? baseShader = null;
        if (shader.BaseShaders.Count > 1) diags.Add(Diagnostic.Create("SD313", $"Multiple base shaders are unsupported for '{shader.Name}'."));
        else if (shader.BaseShaders.Count == 1)
        {
            var baseNameRaw = shader.BaseShaders[0];
            if (baseNameRaw.Contains('<')) diags.Add(Diagnostic.Create("SD314", $"Generic base specialization '{baseNameRaw}' is unsupported."));
            else if (!registry.TryGetValue(baseNameRaw, out baseShader)) diags.Add(Diagnostic.Create("SD310", $"Unresolved base shader '{baseNameRaw}'."));
            else if (baseShader.Name == shader.Name || baseShader.BaseShaders.Contains(shader.Name, StringComparer.Ordinal)) diags.Add(Diagnostic.Create("SD311", $"Inheritance cycle detected for '{shader.Name}'."));
        }

        var mergedStreams = MergeStreams(baseShader, shader, diags);
        var methods = MergeMethods(baseShader, shader, diags);

        var sb = new StringBuilder();
        sb.AppendLine($"// Lowered from {shader.Name}");
        sb.AppendLine("struct StriVStageStreams"); sb.AppendLine("{");
        foreach (var stream in mergedStreams) sb.AppendLine($"    {stream.Type} {stream.Name} : {stream.Semantic};");
        sb.AppendLine("};"); sb.AppendLine();

        var neededHelpers = methods.Values.SelectMany(m => m.BaseCalls).Select(c => c.MethodName).ToHashSet(StringComparer.Ordinal);
        if (baseShader is not null)
        {
            foreach (var bm in baseShader.Methods.Where(m => neededHelpers.Contains(m.Name)))
            {
                sb.AppendLine($"void __base_{baseShader.Name}_{bm.Name}(inout StriVStageStreams streams)");
                sb.AppendLine("{");
                foreach (var line in bm.Body.Split('\n')) sb.AppendLine($"    {line.TrimEnd()}");
                sb.AppendLine("}"); sb.AppendLine();
            }
        }

        foreach (var method in methods.Values)
        {
            var rewritten = RewriteBaseCalls(method, baseShader, methods, diags);
            if (method.Name == "VSMain") { sb.AppendLine("StriVStageStreams VSMain()"); sb.AppendLine("{"); sb.AppendLine("    StriVStageStreams streams;"); foreach (var l in rewritten.Split('\n')) sb.AppendLine($"    {l.TrimEnd()}"); sb.AppendLine("    return streams;"); sb.AppendLine("}"); }
            else if (method.Name == "PSMain") { var suffix = method.ReturnType == "float4" ? " : SV_Target" : string.Empty; sb.AppendLine($"{method.ReturnType} PSMain(StriVStageStreams streams){suffix}"); sb.AppendLine("{"); foreach (var l in rewritten.Split('\n')) sb.AppendLine($"    {l.TrimEnd()}"); sb.AppendLine("}"); }
        }

        return new(sb.ToString(), diags);
    }

    private static string RewriteBaseCalls(SdslStageMethod method, SdslShader? baseShader, Dictionary<string,SdslStageMethod> methods, List<Diagnostic> diags)
    {
        var body = method.Body;
        foreach (var bc in method.BaseCalls.OrderByDescending(x => x.Span.Start))
        {
            if (baseShader is null || !baseShader.Methods.Any(m => m.Name == bc.MethodName))
            {
                diags.Add(Diagnostic.Create("SD312", $"Base method '{bc.MethodName}' not found for '{method.Name}'.", bc.Span.Line, bc.Span.Column));
                continue;
            }
            var replacement = bc.ArgumentCount == 0 ? $"__base_{baseShader.Name}_{bc.MethodName}(streams);" : $"__base_{baseShader.Name}_{bc.MethodName}(streams, {bc.ArgumentText});";
            body = body.Remove(bc.Span.Start, bc.Span.Length).Insert(bc.Span.Start, replacement);
        }
        return body;
    }

    private static Dictionary<string,SdslStageMethod> MergeMethods(SdslShader? baseShader, SdslShader child, List<Diagnostic> diags)
    {
        var map = new Dictionary<string,SdslStageMethod>(StringComparer.Ordinal);
        if (baseShader is not null) foreach (var bm in baseShader.Methods) map[bm.Name] = bm;
        foreach (var cm in child.Methods)
        {
            var hasOverride = cm.Modifiers.Contains("override", StringComparer.Ordinal);
            if (map.ContainsKey(cm.Name) && !hasOverride) diags.Add(Diagnostic.Create("SD313", $"Method '{cm.Name}' must be declared override."));
            if (!map.ContainsKey(cm.Name) && hasOverride && baseShader is not null) diags.Add(Diagnostic.Create("SD312", $"Override method '{cm.Name}' has no base implementation."));
            map[cm.Name] = cm;
        }
        return map;
    }

    private static List<SdslStream> MergeStreams(SdslShader? baseShader, SdslShader child, List<Diagnostic> diags)
    {
        var outList = new List<SdslStream>();
        foreach (var s in baseShader?.Streams ?? []) outList.Add(s);
        foreach (var s in child.Streams)
        {
            var existing = outList.FirstOrDefault(x => x.Name == s.Name);
            if (existing is null)
            {
                if (outList.Any(x => x.Semantic.Equals(s.Semantic, StringComparison.OrdinalIgnoreCase))) diags.Add(Diagnostic.Create("SD315", $"Conflicting stream semantic '{s.Semantic}'.", s.Span.Line, s.Span.Column));
                outList.Add(s);
                continue;
            }
            if (existing.Type != s.Type || existing.Semantic != s.Semantic) diags.Add(Diagnostic.Create("SD315", $"Conflicting stream '{s.Name}'.", s.Span.Line, s.Span.Column));
        }
        return outList;
    }
}
