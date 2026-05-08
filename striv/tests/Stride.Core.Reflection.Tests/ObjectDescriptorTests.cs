using System.Runtime.Serialization;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Reflection.Tests;

public class ObjectDescriptorTests
{
    [DataContract]
    private sealed class DescriptorFixture
    {
        [DataMember(0)]
        public int IncludedField;

        [DataMemberIgnore]
        public int IgnoredField;

        public string? OptionalName { get; set; }
    }

    [Fact]
    public void ObjectDescriptor_IncludesSerializableMembers()
    {
        var descriptor = Assert.IsType<ObjectDescriptor>(TypeDescriptorFactory.Default.Find(typeof(DescriptorFixture)));

        Assert.NotNull(descriptor.TryGetMember(nameof(DescriptorFixture.IncludedField)));
        Assert.NotNull(descriptor.TryGetMember(nameof(DescriptorFixture.OptionalName)));
    }

    [Fact]
    public void ObjectDescriptor_ExcludesIgnoredMembers()
    {
        var descriptor = Assert.IsType<ObjectDescriptor>(TypeDescriptorFactory.Default.Find(typeof(DescriptorFixture)));

        Assert.Null(descriptor.TryGetMember(nameof(DescriptorFixture.IgnoredField)));
    }

    [Fact]
    public void ObjectDescriptor_HandlesNullablePropertyMetadata()
    {
        var descriptor = Assert.IsType<ObjectDescriptor>(TypeDescriptorFactory.Default.Find(typeof(DescriptorFixture)));
        var optionalName = descriptor.TryGetMember(nameof(DescriptorFixture.OptionalName));

        Assert.NotNull(optionalName);
        Assert.Equal(typeof(string), optionalName!.Type);
    }
}
