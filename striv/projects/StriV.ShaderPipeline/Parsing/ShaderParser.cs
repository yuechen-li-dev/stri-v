using System.Text.RegularExpressions;
using StriV.ShaderPipeline.Ast;
using StriV.ShaderPipeline.Diagnostics;

namespace StriV.ShaderPipeline.Parsing;

public sealed class ShaderParser
{
    public ParseResult<HlslDocument> ParseHlsl(string source)
    {
        var diags = new List<Diagnostic>();
        var funcs = new List<HlslFunction>();
        var regex = new Regex(@"([A-Za-z_][\w<>\s\*]*\s+[A-Za-z_]\w*\s*\([^\)]*\)\s*(?::\s*[A-Za-z_]\w*)?)\s*\{", RegexOptions.Multiline);
        foreach (Match m in regex.Matches(source))
        {
            var bodyStart = m.Index + m.Length - 1;
            var body = ReadBalancedBlock(source, bodyStart, out _);
            funcs.Add(new(m.Groups[1].Value.Trim(), body));
        }
        return new(new(source, funcs), diags);
    }

    public ParseResult<SdslShader> ParseSdsl(string source)
    {
        var diags = new List<Diagnostic>();
        var header = Regex.Match(source, @"shader\s+([A-Za-z_]\w*)");
        if (!header.Success) return new(null, [new("Missing shader header", 1, 1)]);
        var name = header.Groups[1].Value;
        var streams = Regex.Matches(source, @"stage\s+stream\s+([^\s]+)\s+([A-Za-z_]\w*)\s*:\s*([A-Za-z_]\w*)\s*;")
            .Select(m => new SdslStream(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value)).ToList();
        var methods = new List<SdslStageMethod>();
        var methodRegex = new Regex(@"stage\s+override\s+([^\s]+)\s+([A-Za-z_]\w*)\s*\(([^\)]*)\)\s*\{", RegexOptions.Multiline);
        foreach (Match m in methodRegex.Matches(source))
        {
            var body = ReadBalancedBlock(source, m.Index + m.Length - 1, out _);
            methods.Add(new(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, body[1..^1].Trim()));
        }
        return new(new(name, streams, methods), diags);
    }

    private static string ReadBalancedBlock(string source, int braceStart, out int end)
    {
        var depth = 0;
        for (var i = braceStart; i < source.Length; i++)
        {
            if (source[i] == '{') depth++;
            else if (source[i] == '}') depth--;
            if (depth == 0) { end = i; return source[braceStart..(i + 1)]; }
        }
        end = source.Length - 1;
        return source[braceStart..];
    }
}
