using System;
using System.IO;
using System.Linq;
using Stride.Core.Serialization;
using Stride.Core.Serialization.Generator.Tests.Fixtures;
using Xunit;

namespace Stride.Core.Serialization.Generator.Tests;

public class SerializationGeneratorFixtureTests
{
    private static void EnsureRegistered()
    {
        var registrar = typeof(FixtureContract).Assembly.GetType("Stride.Core.DataSerializers.GeneratedSerializationRegistrar");
        var method = registrar?.GetMethod("RegisterForTests", System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public);
        method?.Invoke(null, null);
    }

    [Fact]
    public void Generator_EmitsSerializer_ForFixtureDataContract()
    {
        var generatedType = typeof(FixtureContract).Assembly.GetType("Stride.Core.DataSerializers.FixtureContractDataSerializer");
        Assert.NotNull(generatedType);
    }

    [Fact]
    public void GeneratedSerializer_IsDiscoverable_ForFixtureType()
    {
        EnsureRegistered();
        var serializer = SerializerSelector.Default.GetSerializer<FixtureContract>();
        Assert.NotNull(serializer);
    }

    [Fact]
    public void GeneratedSerializer_RoundTrips_PrimitiveMembers()
    {
        EnsureRegistered();
        var source = new FixtureContract { Number = 42, Ratio = 0.5f, Flag = true, Name = "m11c", Ignored = 999 };
        var result = SerializerExtensions.Clone(source);

        Assert.Equal(42, result.Number);
        Assert.Equal(0.5f, result.Ratio);
        Assert.True(result.Flag);
        Assert.Equal("m11c", result.Name);
    }

    [Fact]
    public void GeneratedSerializer_RespectsDataMemberIgnore()
    {
        EnsureRegistered();
        var source = new FixtureContract { Number = 1, Ratio = 2, Flag = false, Name = "x", Ignored = 777 };
        var clone = SerializerExtensions.Clone(source);
        Assert.Equal(0, clone.Ignored);
    }
}
