namespace StriV.ShaderPipeline.Lexing;

public readonly record struct Token(TokenKind Kind, string Text, SourceSpan Span);
