using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;
using Dominatus.OptFlow;
using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Nodes;

public static class SceneLifecycleNode
{
    public static RootSceneSetRequested RequestRootSceneSet(SceneInstance sceneInstance, Scene rootScene) => new(sceneInstance, rootScene);

    public static RootSceneClearRequested RequestRootSceneClear(SceneInstance sceneInstance) => new(sceneInstance);

    public static EntitySceneAttachRequested RequestEntityAttach(Entity entity, Scene scene) => new(entity, scene);

    public static EntitySceneDetachRequested RequestEntityDetach(Entity entity) => new(entity);

    public static ValueTask<EntitySceneAttached> ExecuteEntityAttachAsync(
        EntitySceneAttachRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => SceneLifecycleTransition.AttachEntityAsync(request, actuator, cancellationToken);

    public static ValueTask<RootSceneSet> ExecuteRootSceneSetAsync(
        RootSceneSetRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => SceneLifecycleTransition.SetRootSceneAsync(request, actuator, cancellationToken);

    public static ValueTask<RootSceneCleared> ExecuteRootSceneClearAsync(
        RootSceneClearRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => SceneLifecycleTransition.ClearRootSceneAsync(request, actuator, cancellationToken);

    public static ValueTask<EntitySceneDetached> ExecuteEntityDetachAsync(
        EntitySceneDetachRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => SceneLifecycleTransition.DetachEntityAsync(request, actuator, cancellationToken);

    public static IEnumerator<AiStep> Idle(AiCtx _)
    {
        while (true)
            yield return Ai.Wait(0.1f);
    }
}
