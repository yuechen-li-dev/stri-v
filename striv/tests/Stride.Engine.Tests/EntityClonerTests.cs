using Xunit;

namespace Stride.Engine.Tests;

public class EntityClonerTests
{
    [Fact]
    public void EntityCloner_Clone_NullEntity_ThrowsArgumentNullException()
    {
        Assert.Throws<ArgumentNullException>(() => Design.EntityCloner.Clone(null!));
    }

    [Fact]
    public void EntityCloner_Clone_WithoutSerializerRegistration_ThrowsArgumentException()
    {
        var source = new Entity("Source");

        var exception = Assert.Throws<ArgumentException>(() => Design.EntityCloner.Clone(source));

        Assert.Contains("No serializer available", exception.Message);
    }
}
