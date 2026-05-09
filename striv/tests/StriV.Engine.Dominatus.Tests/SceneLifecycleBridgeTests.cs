using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class SceneLifecycleBridgeTests
{
    [Fact]
    public async Task SceneLifecycleTransition_AttachEntity_InvokesActuatorAndReturnsAttachedEvent()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new FakeSceneLifecycleActuator();
        var request = new EntitySceneAttachRequested(entity, scene);

        var completed = await SceneLifecycleTransition.AttachEntityAsync(request, actuator);

        Assert.Same(entity, completed.Entity);
        Assert.Same(scene, completed.Scene);
        Assert.Equal(1, actuator.AttachCalls);
        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public async Task SceneLifecycleTransition_DetachEntity_InvokesActuatorAndReturnsDetachedEvent()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new FakeSceneLifecycleActuator();

        await actuator.AttachEntityToSceneAsync(entity, scene);
        var request = new EntitySceneDetachRequested(entity);

        var completed = await SceneLifecycleTransition.DetachEntityAsync(request, actuator);

        Assert.Same(entity, completed.Entity);
        Assert.Equal(1, actuator.DetachCalls);
        Assert.Null(entity.Scene);
        Assert.DoesNotContain(entity, scene.Entities);
    }

    [Fact]
    public async Task SceneLifecycleTransition_AttachEntity_PropagatesActuatorFailure()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var request = new EntitySceneAttachRequested(entity, scene);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await SceneLifecycleTransition.AttachEntityAsync(request, new ThrowingSceneLifecycleActuator()));

        Assert.Equal("attach-failed", ex.Message);
    }

    [Fact]
    public async Task SceneLifecycleTransition_RejectsNullActuator()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var attachRequest = new EntitySceneAttachRequested(entity, scene);
        var detachRequest = new EntitySceneDetachRequested(entity);

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await SceneLifecycleTransition.AttachEntityAsync(attachRequest, actuator: null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await SceneLifecycleTransition.DetachEntityAsync(detachRequest, actuator: null!));
    }

    [Fact]
    public void SceneLifecycleEvents_CarryExpectedPayloads()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();

        var attachRequested = new EntitySceneAttachRequested(entity, scene);
        var attached = new EntitySceneAttached(entity, scene);
        var detachRequested = new EntitySceneDetachRequested(entity);
        var detached = new EntitySceneDetached(entity);

        Assert.Same(entity, attachRequested.Entity);
        Assert.Same(scene, attachRequested.Scene);
        Assert.Same(entity, attached.Entity);
        Assert.Same(scene, attached.Scene);
        Assert.Same(entity, detachRequested.Entity);
        Assert.Same(entity, detached.Entity);
    }

    [Fact]
    public async Task SceneLifecycleNode_Surface_ExposesEntitySceneLifecycleIntent()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new FakeSceneLifecycleActuator();

        var attachRequest = SceneLifecycleNode.RequestEntityAttach(entity, scene);
        var detachRequest = SceneLifecycleNode.RequestEntityDetach(entity);

        Assert.Same(entity, attachRequest.Entity);
        Assert.Same(scene, attachRequest.Scene);
        Assert.Same(entity, detachRequest.Entity);

        var attached = await SceneLifecycleNode.ExecuteEntityAttachAsync(attachRequest, actuator);
        var detached = await SceneLifecycleNode.ExecuteEntityDetachAsync(detachRequest, actuator);

        Assert.Same(entity, attached.Entity);
        Assert.Same(scene, attached.Scene);
        Assert.Same(entity, detached.Entity);
        Assert.Equal(1, actuator.AttachCalls);
        Assert.Equal(1, actuator.DetachCalls);
    }

    private sealed class FakeSceneLifecycleActuator : ISceneLifecycleActuator
    {
        public int AttachCalls { get; private set; }
        public int DetachCalls { get; private set; }

        public ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask AttachEntityToSceneAsync(Entity entity, Scene scene, CancellationToken cancellationToken = default)
        {
            AttachCalls++;
            entity.Scene = scene;
            return ValueTask.CompletedTask;
        }

        public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default)
        {
            DetachCalls++;
            DetachFromScene(entity);
            return ValueTask.CompletedTask;
        }

        private static void DetachFromScene(Entity entity)
        {
            // Legacy Stride detach API: null scene means detach. Kept inside fake actuator boundary.
            entity.Scene = null!;
        }
    }

    private sealed class ThrowingSceneLifecycleActuator : ISceneLifecycleActuator
    {
        public ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask AttachEntityToSceneAsync(Entity entity, Scene scene, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("attach-failed");

        public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("detach-failed");
    }
}
