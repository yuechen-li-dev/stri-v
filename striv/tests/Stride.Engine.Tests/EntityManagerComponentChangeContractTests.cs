using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class EntityManagerComponentChangeContractTests
{
    [Fact]
    public void EntityComponentChange_Added_HasExpectedPayload()
    {
        var entity = new Entity("Entity");
        var component = new TestComponent();

        var change = EntityComponentChange.Added(entity, component);

        Assert.Same(entity, change.Entity);
        Assert.Equal(EntityComponentChangeKind.Added, change.Kind);
        Assert.Null(change.OldComponent);
        Assert.Same(component, change.NewComponent);
        Assert.Same(component, change.AddedComponent);
    }

    [Fact]
    public void EntityComponentChange_Removed_HasExpectedPayload()
    {
        var entity = new Entity("Entity");
        var component = new TestComponent();

        var change = EntityComponentChange.Removed(entity, component);

        Assert.Same(entity, change.Entity);
        Assert.Equal(EntityComponentChangeKind.Removed, change.Kind);
        Assert.Same(component, change.OldComponent);
        Assert.Null(change.NewComponent);
        Assert.Same(component, change.RemovedComponent);
    }

    [Fact]
    public void EntityComponentChange_Replaced_HasExpectedPayload()
    {
        var entity = new Entity("Entity");
        var oldComponent = new TestComponent();
        var newComponent = new TestComponent();

        var change = EntityComponentChange.Replaced(entity, oldComponent, newComponent);

        Assert.Same(entity, change.Entity);
        Assert.Equal(EntityComponentChangeKind.Replaced, change.Kind);
        Assert.Same(oldComponent, change.OldComponent);
        Assert.Same(newComponent, change.NewComponent);
        Assert.Same(oldComponent, change.RemovedComponent);
        Assert.Same(newComponent, change.AddedComponent);
    }

    [Fact]
    public void EntityComponentChange_InvalidRequiredAccess_ThrowsDeterministicException()
    {
        var entity = new Entity("Entity");
        var component = new TestComponent();
        var added = EntityComponentChange.Added(entity, component);
        var removed = EntityComponentChange.Removed(entity, component);

        var addedException = Assert.Throws<InvalidOperationException>(() => _ = added.RemovedComponent);
        var removedException = Assert.Throws<InvalidOperationException>(() => _ = removed.AddedComponent);

        Assert.Equal("Component change does not contain a removed component.", addedException.Message);
        Assert.Equal("Component change does not contain an added component.", removedException.Message);
    }

    private sealed class TestComponent : EntityComponent;
}
