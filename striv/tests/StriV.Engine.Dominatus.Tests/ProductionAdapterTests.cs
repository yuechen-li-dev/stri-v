using Stride.Engine;
using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class ProductionAdapterTests
{
    [Fact]
    public async Task StrideTransformLifecycleActuator_AttachParent_UsesStrideParenting()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleActuator();

        await actuator.AttachParentAsync(child, parent);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideTransformLifecycleActuator_DetachParent_UsesStrideDetach()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleActuator();

        await actuator.AttachParentAsync(child, parent);
        await actuator.DetachParentAsync(child);

        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideSceneLifecycleActuator_AttachEntity_UsesStrideSceneMembership()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new StrideSceneLifecycleActuator();

        await actuator.AttachEntityToSceneAsync(entity, scene);

        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public async Task StrideSceneLifecycleActuator_DetachEntity_UsesStrideSceneDetach()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new StrideSceneLifecycleActuator();

        await actuator.AttachEntityToSceneAsync(entity, scene);
        await actuator.DetachEntityFromSceneAsync(entity);

        Assert.Null(entity.Scene);
        Assert.DoesNotContain(entity, scene.Entities);
    }

    [Fact]
    public async Task ProductionAdapters_WorkThroughTransitionHelpers()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var sceneEntity = new Entity("SceneEntity");
        var scene = new Scene();
        var transformActuator = new StrideTransformLifecycleActuator();
        var sceneActuator = new StrideSceneLifecycleActuator();

        var parentAttached = await TransformLifecycleTransition.AttachParentAsync(
            new TransformParentAttachRequested(child, parent),
            transformActuator);
        var entityAttached = await SceneLifecycleTransition.AttachEntityAsync(
            new EntitySceneAttachRequested(sceneEntity, scene),
            sceneActuator);

        Assert.Same(child, parentAttached.Child);
        Assert.Same(parent, parentAttached.Parent);
        Assert.Same(parent.Transform, child.Transform.Parent);

        Assert.Same(sceneEntity, entityAttached.Entity);
        Assert.Same(scene, entityAttached.Scene);
        Assert.Same(scene, sceneEntity.Scene);
    }

    [Fact]
    public async Task StrideTransformLifecycleActuator_RejectsNullArguments()
    {
        var actuator = new StrideTransformLifecycleActuator();
        var entity = new Entity("Entity");

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.AttachParentAsync(null!, entity));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.AttachParentAsync(entity, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.DetachParentAsync(null!));
    }

    [Fact]
    public async Task StrideSceneLifecycleActuator_RejectsNullArguments()
    {
        var actuator = new StrideSceneLifecycleActuator();
        var entity = new Entity("Entity");
        var scene = new Scene();

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.AttachSceneAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.DetachSceneAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.AttachEntityToSceneAsync(null!, scene));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.AttachEntityToSceneAsync(entity, null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await actuator.DetachEntityFromSceneAsync(null!));
    }
}
