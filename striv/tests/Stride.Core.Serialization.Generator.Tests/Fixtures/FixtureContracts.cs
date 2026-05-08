using Stride.Core;

namespace Stride.Core.Serialization.Generator.Tests.Fixtures;

[DataContract]
public class FixtureContract
{
    [DataMember(0)] public int Number;
    [DataMember(1)] public float Ratio { get; set; }
    [DataMember(2)] public bool Flag { get; set; }
    [DataMember(3)] public string Name { get; set; } = string.Empty;
    [DataMemberIgnore] public int Ignored;
}
