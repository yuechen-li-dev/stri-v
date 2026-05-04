namespace StriV.ShaderPipeline.Ast;

public sealed record HlslDocument(string Source, IReadOnlyList<HlslFunction> Functions);
public sealed record HlslFunction(string Signature, string Body);
public sealed record SdslShader(string Name, IReadOnlyList<SdslStream> Streams, IReadOnlyList<SdslStageMethod> Methods);
public sealed record SdslStream(string Type, string Name, string Semantic);
public sealed record SdslStageMethod(string ReturnType, string Name, string Parameters, string Body);
