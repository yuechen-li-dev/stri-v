using Stride.Core.Mathematics;
using Stride.Core;
using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public class EntityLifecycleTests
{
    [Fact]
    public void Entity_DefaultConstruction_HasValidInertState()
    {
        var entity = new Entity();

        Assert.NotNull(entity.Transform);
        Assert.NotNull(entity.Components);
        Assert.Single(entity.Components);
        Assert.Same(entity.Transform, Assert.IsType<TransformComponent>(entity.Components[0]));
        Assert.Null(entity.Scene);
        Assert.Null(entity.EntityManager);
    }

    [Fact]
    public void EntityComponent_DefaultConstruction_IsUnattached()
    {
        var component = new TestComponent();

        Assert.Null(component.Entity);
    }

    [Fact]
    public void EntityComponentCollection_AddRemove_UpdatesComponentEntityLink()
    {
        var entity = new Entity();
        var component = new TestComponent();

        entity.Components.Add(component);
        Assert.Same(entity, component.Entity);

        var removed = entity.Components.Remove(component);
        Assert.True(removed);
        Assert.Null(component.Entity);

        var removedAgain = entity.Components.Remove(component);
        Assert.False(removedAgain);
    }

    [Fact]
    public void TransformComponent_ParentDetach_ClearsParentAndChildLink()
    {
        var parent = new Entity("parent");
        var child = new Entity("child");

        child.Transform.Parent = parent.Transform;
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);

        parent.Transform.Children.Remove(child.Transform);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public void EntityManager_AddRemoveEntity_UpdatesEntityManagerLink()
    {
        var instance = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        var entity = new Entity("entity");

        instance.RootScene.Entities.Add(entity);
        Assert.Same(instance, entity.EntityManager);

        instance.RootScene.Entities.Remove(entity);
        Assert.Null(entity.EntityManager);
    }

    private sealed class TestComponent : EntityComponent;
}
