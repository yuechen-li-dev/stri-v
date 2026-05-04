namespace StriV.ShaderPipeline.Lexing;

public readonly record struct SourceSpan(int Start, int Length, int Line, int Column);
