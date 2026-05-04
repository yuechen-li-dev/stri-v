using System.Text.RegularExpressions;
using StriV.ShaderPipeline.Ast;
using StriV.ShaderPipeline.Diagnostics;
using StriV.ShaderPipeline.Lexing;

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
        var header = Regex.Match(source, @"shader\s+([A-Za-z_]\w*)(\s*<([^>]+)>)?(\s*:\s*([^\{\r\n]+))?");
        if (!header.Success) return new(null, [Diagnostic.Create("SD000", "Missing shader header")]);
        var name = header.Groups[1].Value;
        var genericParametersText = header.Groups[3].Success ? header.Groups[3].Value.Trim() : null;
        var baseShaders = header.Groups[5].Success
            ? header.Groups[5].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).ToList()
            : new List<string>();

        if (!string.IsNullOrWhiteSpace(genericParametersText))
            diags.Add(Diagnostic.Create("SD301", "Generic parameters parsed but specialization is not implemented.", GetSpan(source, header.Groups[3].Index).Line, GetSpan(source, header.Groups[3].Index).Column));
        if (baseShaders.Count > 0)
            diags.Add(Diagnostic.Create("SD300", "Shader inheritance parsed but mixin merge is not implemented.", GetSpan(source, header.Groups[5].Index).Line, GetSpan(source, header.Groups[5].Index).Column));

        var streams = new List<SdslStream>();
        var streamRegex = new Regex(@"stage\s+stream\s+([^\s]+)\s+([A-Za-z_]\w*)\s*:\s*([A-Za-z_]\w*)\s*;", RegexOptions.Multiline);
        foreach (Match m in streamRegex.Matches(source))
        {
            var span = GetSpan(source, m.Index);
            streams.Add(new(m.Groups[1].Value, m.Groups[2].Value, m.Groups[3].Value, span));
        }

        var methods = new List<SdslStageMethod>();
        var methodRegex = new Regex(@"(stage\s+override)\s+([^\s]+)\s+([A-Za-z_]\w*)\s*\(([^\)]*)\)\s*\{", RegexOptions.Multiline);
        foreach (Match m in methodRegex.Matches(source))
        {
            var body = ReadBalancedBlock(source, m.Index + m.Length - 1, out _);
            var span = GetSpan(source, m.Index);
            methods.Add(new(m.Groups[2].Value, m.Groups[3].Value, m.Groups[4].Value, body[1..^1].Trim(), new[] { "stage", "override" }, span));
        }

        return new(new(name, genericParametersText, baseShaders, streams, methods, new[] { "shader" }), diags);
    }

    private static SourceSpan GetSpan(string source, int index)
    {
        var line = 1;
        var col = 1;
        for (var i = 0; i < index; i++)
        {
            if (source[i] == '\n') { line++; col = 1; }
            else col++;
        }
        return new(index, 1, line, col);
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
