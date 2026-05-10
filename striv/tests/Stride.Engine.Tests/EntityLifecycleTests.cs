using System;
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
        Assert.False(entity.IsManaged);
    }

    [Fact]
    public void EntityComponent_DefaultConstruction_IsUnattached()
    {
        var component = new TestComponent();

        Assert.False(component.IsAttached);
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
        Assert.False(component.IsAttached);

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
        Assert.False(entity.IsManaged);
    }

    [Fact]
    public void EntityComponent_UnattachedEntityAccess_ThrowsInvalidOperationException()
    {
        var component = new TestComponent();
        var ex = Assert.Throws<InvalidOperationException>(() => _ = component.Entity);
        Assert.Contains("not attached", ex.Message);
    }

    [Fact]
    public void EntityComponent_AttachedEntityAccess_ReturnsOwner()
    {
        var entity = new Entity();
        var component = new TestComponent();
        entity.Components.Add(component);
        Assert.Same(entity, component.Entity);
    }

    [Fact]
    public void EntityComponent_RemovedEntityAccess_ThrowsInvalidOperationException()
    {
        var entity = new Entity();
        var component = new TestComponent();
        entity.Components.Add(component);
        entity.Components.Remove(component);
        var ex = Assert.Throws<InvalidOperationException>(() => _ = component.Entity);
        Assert.Contains("not attached", ex.Message);
    }

    [Fact]
    public void Entity_UnmanagedEntityManagerAccess_ThrowsInvalidOperationException()
    {
        var entity = new Entity("entity");
        var ex = Assert.Throws<InvalidOperationException>(() => _ = entity.EntityManager);
        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void Entity_ManagedEntityManagerAccess_ReturnsManager()
    {
        var instance = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        var entity = new Entity("entity");
        instance.RootScene.Entities.Add(entity);
        Assert.Same(instance, entity.EntityManager);
    }

    [Fact]
    public void Entity_RemovedEntityManagerAccess_ThrowsInvalidOperationException()
    {
        var instance = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        var entity = new Entity("entity");
        instance.RootScene.Entities.Add(entity);
        instance.RootScene.Entities.Remove(entity);
        var ex = Assert.Throws<InvalidOperationException>(() => _ = entity.EntityManager);
        Assert.Contains("not registered", ex.Message);
    }

    [Fact]
    public void EntityProcessor_UnboundLifecycleAccess_ThrowsInvalidOperationException()
    {
        var processor = new TestProcessor();
        var managerEx = Assert.Throws<InvalidOperationException>(() => _ = processor.EntityManager);
        var servicesEx = Assert.Throws<InvalidOperationException>(() => _ = processor.ServicesAccessor);
        Assert.Contains("not attached", managerEx.Message);
        Assert.Contains("services are not available", servicesEx.Message);
    }

    private sealed class TestComponent : EntityComponent;

    private sealed class TestProcessor : EntityProcessor
    {
        public TestProcessor() : base(typeof(TestComponent), Array.Empty<Type>()) { }
        public IServiceRegistry ServicesAccessor => Services;
        protected internal override void OnSystemAdd() { }
        protected internal override void OnSystemRemove() { }
        protected internal override void RemoveAllEntities() { }
        protected internal override void ProcessEntityComponent(Entity entity, EntityComponent entityComponent, bool forceRemove) { }
    }
}
