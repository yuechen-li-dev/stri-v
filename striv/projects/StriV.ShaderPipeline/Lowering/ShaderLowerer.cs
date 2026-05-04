using System.Text;
using StriV.ShaderPipeline.Ast;

namespace StriV.ShaderPipeline.Lowering;

public sealed class ShaderLowerer
{
    public string EmitHlsl(HlslDocument document) => document.Source;

    public string LowerSdslToHlsl(SdslShader shader)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"// Lowered from {shader.Name}");
        sb.AppendLine("struct StageStreams");
        sb.AppendLine("{");
        foreach (var stream in shader.Streams) sb.AppendLine($"    {stream.Type} {stream.Name} : {stream.Semantic};");
        sb.AppendLine("};");
        sb.AppendLine("static StageStreams __streams;");
        foreach (var method in shader.Methods)
        {
            var body = method.Body.Replace("streams.", "__streams.");
            sb.AppendLine($"{method.ReturnType} {method.Name}({method.Parameters})");
            sb.AppendLine("{");
            sb.AppendLine(body);
            sb.AppendLine("}");
        }
        return sb.ToString();
    }
}
