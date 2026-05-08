using System.Collections;
using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Reflection.Tests;

public class TypeDescriptorFactoryCollectionFallbackTests
{
    [Fact]
    public void TypeDescriptorFactory_UsesSpecificDescriptor_ForGenericList()
    {
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(List<int>));

        Assert.IsType<ListDescriptor>(descriptor);
        Assert.IsNotType<OldCollectionDescriptor>(descriptor);
    }

    [Fact]
    public void TypeDescriptorFactory_UsesSpecificDescriptor_ForDictionary()
    {
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(Dictionary<string, int>));

        Assert.IsType<DictionaryDescriptor>(descriptor);
    }

    [Fact]
    public void TypeDescriptorFactory_UsesModernDescriptor_ForLegacyICollectionShape()
    {
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(LegacyIntCollection));

        Assert.IsType<GenericCollectionDescriptor>(descriptor);
        Assert.IsNotType<OldCollectionDescriptor>(descriptor);
    }

    [Fact]
    public void ModernCollectionDescriptor_FallbackBehavior_MatchesLegacyICollectionShape()
    {
        var descriptor = Assert.IsType<GenericCollectionDescriptor>(TypeDescriptorFactory.Default.Find(typeof(LegacyIntCollection)));
        var collection = new LegacyIntCollection();

        descriptor.Add(collection, 10);
        descriptor.Add(collection, 20);
        Assert.Equal(2, descriptor.GetCollectionCount(collection));

        descriptor.Remove(collection, 10);
        Assert.Equal(1, descriptor.GetCollectionCount(collection));

        descriptor.Clear(collection);
        Assert.Equal(0, descriptor.GetCollectionCount(collection));
        Assert.Equal(typeof(int), descriptor.ElementType);
        Assert.False(descriptor.HasIndexerAccessors);
    }

    [Fact]
    public void OldCollectionDescriptor_IsNotSelected_ForKnownFallbackShapes()
    {
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(LegacyIntCollection));

        Assert.IsNotType<OldCollectionDescriptor>(descriptor);
    }

    private sealed class LegacyIntCollection : ICollection<int>
    {
        private readonly List<int> inner = [];

        public IEnumerator<int> GetEnumerator() => inner.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => inner.GetEnumerator();

        public void Add(int item) => inner.Add(item);

        public void Clear() => inner.Clear();

        public bool Contains(int item) => inner.Contains(item);

        public void CopyTo(int[] array, int arrayIndex) => inner.CopyTo(array, arrayIndex);

        public bool Remove(int item) => inner.Remove(item);

        public int Count => inner.Count;

        public bool IsReadOnly => false;
    }
}
