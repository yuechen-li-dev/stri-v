using Stride.Core.Reflection;
using Xunit;

namespace Stride.Core.Reflection.Tests;

public class MemberPathTests
{
    private sealed class PathRoot
    {
        public PathChild? Child { get; set; }
        public List<int> Numbers { get; } = [];
        public Dictionary<string, int> Map { get; } = [];
    }

    private sealed class PathChild
    {
        public string? Name { get; set; }
    }

    [Fact]
    public void MemberPath_ValueSet_SetsNestedProperty()
    {
        var root = new PathRoot { Child = new PathChild() };
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(PathRoot));
        var childDescriptor = TypeDescriptorFactory.Default.Find(typeof(PathChild));

        var path = new MemberPath();
        path.Push(descriptor[nameof(PathRoot.Child)]);
        path.Push(childDescriptor[nameof(PathChild.Name)]);

        var applied = path.Apply(root, MemberPathAction.ValueSet, "updated");

        Assert.True(applied);
        Assert.Equal("updated", root.Child!.Name);
    }

    [Fact]
    public void MemberPath_ValueSet_NullIntermediate_FailsPredictably()
    {
        var root = new PathRoot();
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(PathRoot));
        var childDescriptor = TypeDescriptorFactory.Default.Find(typeof(PathChild));

        var path = new MemberPath();
        path.Push(descriptor[nameof(PathRoot.Child)]);
        path.Push(childDescriptor[nameof(PathChild.Name)]);

        Assert.False(path.Apply(root, MemberPathAction.ValueSet, "updated"));
        Assert.Null(root.Child);
    }

    [Fact]
    public void MemberPath_CollectionAdd_AddsItem()
    {
        var root = new PathRoot();
        var descriptor = TypeDescriptorFactory.Default.Find(typeof(PathRoot));
        var numbersDescriptor = TypeDescriptorFactory.Default.Find(typeof(List<int>));

        var path = new MemberPath();
        path.Push(descriptor[nameof(PathRoot.Numbers)]);
        path.Push((CollectionDescriptor)numbersDescriptor, 0);

        Assert.True(path.Apply(root, MemberPathAction.CollectionAdd, 7));
        Assert.Equal([7], root.Numbers);
    }

    [Fact]
    public void MemberPath_CollectionRemove_RemovesItem()
    {
        var root = new PathRoot();
        root.Numbers.Add(3);
        root.Numbers.Add(7);

        var descriptor = TypeDescriptorFactory.Default.Find(typeof(PathRoot));
        var numbersDescriptor = TypeDescriptorFactory.Default.Find(typeof(List<int>));

        var path = new MemberPath();
        path.Push(descriptor[nameof(PathRoot.Numbers)]);
        path.Push((CollectionDescriptor)numbersDescriptor, 0);

        Assert.True(path.Apply(root, MemberPathAction.CollectionRemove, null));
        Assert.Equal([7], root.Numbers);
    }

    [Fact]
    public void MemberPath_DictionaryRemove_RemovesKey()
    {
        var root = new PathRoot();
        root.Map["a"] = 1;

        var descriptor = TypeDescriptorFactory.Default.Find(typeof(PathRoot));
        var mapDescriptor = TypeDescriptorFactory.Default.Find(typeof(Dictionary<string, int>));

        var path = new MemberPath();
        path.Push(descriptor[nameof(PathRoot.Map)]);
        path.Push((DictionaryDescriptor)mapDescriptor, "a");

        Assert.True(path.Apply(root, MemberPathAction.DictionaryRemove, null));
        Assert.False(root.Map.ContainsKey("a"));
    }
}
