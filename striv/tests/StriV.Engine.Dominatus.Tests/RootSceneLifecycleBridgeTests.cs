using Stride.Core;
using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class RootSceneLifecycleBridgeTests
{
    [Fact]
    public async Task RootSceneLifecycleTransition_SetRootScene_InvokesActuatorAndReturnsCompletedEvent()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var rootEntity = new Entity("RootEntity");
        rootScene.Entities.Add(rootEntity);
        var actuator = new StrideSceneLifecycleActuator();

        var completed = await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), actuator);

        Assert.Same(sceneInstance, completed.SceneInstance);
        Assert.Same(rootScene, completed.RootScene);
        Assert.Same(rootScene, sceneInstance.RootScene);
        Assert.Same(sceneInstance, rootEntity.EntityManager);
    }

    [Fact]
    public async Task RootSceneLifecycleTransition_ClearRootScene_InvokesActuatorAndReturnsCompletedEvent()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var rootEntity = new Entity("RootEntity");
        rootScene.Entities.Add(rootEntity);
        var actuator = new StrideSceneLifecycleActuator();
        await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), actuator);

        var completed = await SceneLifecycleTransition.ClearRootSceneAsync(new RootSceneClearRequested(sceneInstance), actuator);

        Assert.Same(sceneInstance, completed.SceneInstance);
        Assert.Null(sceneInstance.RootScene);
        Assert.Null(rootEntity.EntityManager);
    }

    [Fact]
    public async Task RootSceneLifecycleTransition_SetRootScene_PropagatesActuatorFailure()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), new ThrowingRootSceneLifecycleActuator()));

        Assert.Equal("set-root-failed", ex.Message);
    }

    [Fact]
    public async Task RootSceneLifecycleTransition_RejectsNullActuator()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), actuator: null!));

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await SceneLifecycleTransition.ClearRootSceneAsync(new RootSceneClearRequested(sceneInstance), actuator: null!));
    }

    [Fact]
    public async Task SceneLifecycleNode_Surface_ExposesRootSceneLifecycleIntent()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var actuator = new StriV.Engine.Dominatus.Tests.Adapters.StrideSceneLifecycleTestAdapter();

        var setRequest = SceneLifecycleNode.RequestRootSceneSet(sceneInstance, rootScene);
        var clearRequest = SceneLifecycleNode.RequestRootSceneClear(sceneInstance);

        Assert.Same(sceneInstance, setRequest.SceneInstance);
        Assert.Same(rootScene, setRequest.RootScene);
        Assert.Same(sceneInstance, clearRequest.SceneInstance);

        var setCompleted = await SceneLifecycleNode.ExecuteRootSceneSetAsync(setRequest, actuator);
        var clearCompleted = await SceneLifecycleNode.ExecuteRootSceneClearAsync(clearRequest, actuator);

        Assert.Same(sceneInstance, setCompleted.SceneInstance);
        Assert.Same(rootScene, setCompleted.RootScene);
        Assert.Same(sceneInstance, clearCompleted.SceneInstance);
        Assert.Equal(1, actuator.SetRootSceneCalls);
        Assert.Equal(1, actuator.ClearRootSceneCalls);
    }

    private sealed class ThrowingRootSceneLifecycleActuator : ISceneLifecycleActuator
    {
        public ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask AttachEntityToSceneAsync(Entity entity, Scene scene, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
        public ValueTask SetRootSceneAsync(SceneInstance sceneInstance, Scene rootScene, CancellationToken cancellationToken = default) => throw new InvalidOperationException("set-root-failed");
        public ValueTask ClearRootSceneAsync(SceneInstance sceneInstance, CancellationToken cancellationToken = default) => throw new InvalidOperationException("clear-root-failed");
    }
}
