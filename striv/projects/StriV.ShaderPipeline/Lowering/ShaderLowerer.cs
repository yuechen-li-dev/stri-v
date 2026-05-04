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

    public LoweringResult LowerSdslToHlsl(SdslShader shader)
    {
        var diags = new List<Diagnostic>();
        var layout = BuildLayout(shader, diags);

        var sb = new StringBuilder();
        sb.AppendLine($"// Lowered from {shader.Name}");
        sb.AppendLine("struct StriVStageStreams");
        sb.AppendLine("{");
        foreach (var stream in layout.Bindings) sb.AppendLine($"    {stream.Type} {stream.Name} : {stream.Semantic};");
        sb.AppendLine("};");
        sb.AppendLine();

        foreach (var method in shader.Methods)
        {
            if (method.Name == "VSMain")
            {
                LowerVsMain(sb, method, diags);
            }
            else if (method.Name == "PSMain")
            {
                LowerPsMain(sb, method, diags);
            }
            else
            {
                diags.Add(Diagnostic.Create("SD203", $"Unsupported stage method name '{method.Name}'.", method.Span.Line, method.Span.Column));
                sb.AppendLine($"// TODO(SD203): unsupported stage method '{method.Name}'");
                sb.AppendLine($"{method.ReturnType} {method.Name}({method.Parameters})");
                sb.AppendLine("{");
                sb.AppendLine(method.Body);
                sb.AppendLine("}");
            }
            sb.AppendLine();
        }

        return new(sb.ToString(), diags);
    }

    private static StreamLayout BuildLayout(SdslShader shader, List<Diagnostic> diags)
    {
        var seenNames = new HashSet<string>(StringComparer.Ordinal);
        var seenSemantics = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var bindings = new List<StreamBinding>();
        foreach (var s in shader.Streams)
        {
            if (!seenNames.Add(s.Name))
                diags.Add(Diagnostic.Create("SD200", $"Duplicate stream name '{s.Name}'.", s.Span.Line, s.Span.Column));
            if (!seenSemantics.Add(s.Semantic))
                diags.Add(Diagnostic.Create("SD201", $"Duplicate stream semantic '{s.Semantic}'.", s.Span.Line, s.Span.Column));
            bindings.Add(new(s.Type, s.Name, s.Semantic, s.Span.Line, s.Span.Column));
        }
        return new(bindings, diags);
    }

    private static void LowerVsMain(StringBuilder sb, SdslStageMethod method, List<Diagnostic> diags)
    {
        if (!string.IsNullOrWhiteSpace(method.Parameters))
            diags.Add(Diagnostic.Create("SD204", "VSMain parameters are currently not supported and were dropped.", method.Span.Line, method.Span.Column));
        if (method.Body.Contains("return ", StringComparison.Ordinal))
            diags.Add(Diagnostic.Create("SD205", "Existing return statement in VSMain may conflict with generated return streams.", method.Span.Line, method.Span.Column));

        sb.AppendLine("StriVStageStreams VSMain()");
        sb.AppendLine("{");
        sb.AppendLine("    StriVStageStreams streams;");
        foreach (var line in method.Body.Split('\n')) sb.AppendLine($"    {line.TrimEnd()}");
        sb.AppendLine("    return streams;");
        sb.AppendLine("}");
    }

    private static void LowerPsMain(StringBuilder sb, SdslStageMethod method, List<Diagnostic> diags)
    {
        if (!string.IsNullOrWhiteSpace(method.Parameters))
            diags.Add(Diagnostic.Create("SD204", "PSMain parameters are currently not supported and were replaced.", method.Span.Line, method.Span.Column));
        var suffix = method.ReturnType == "float4" ? " : SV_Target" : string.Empty;
        sb.AppendLine($"{method.ReturnType} PSMain(StriVStageStreams streams){suffix}");
        sb.AppendLine("{");
        foreach (var line in method.Body.Split('\n')) sb.AppendLine($"    {line.TrimEnd()}");
        sb.AppendLine("}");
    }
}
