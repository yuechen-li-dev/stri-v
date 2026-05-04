namespace StriV.ShaderPipeline.Lexing;

public enum TokenKind
{
    Identifier,
    Keyword,
    NumericLiteral,
    StringLiteral,
    Punctuation,
    Operator,
    Comment,
    PreprocessorDirective,
    EndOfFile,
}
