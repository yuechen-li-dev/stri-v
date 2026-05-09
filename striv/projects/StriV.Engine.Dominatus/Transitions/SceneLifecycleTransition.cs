using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Transitions;

public static class SceneLifecycleTransition
{
    public static async ValueTask<RootSceneSet> SetRootSceneAsync(
        RootSceneSetRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.SceneInstance);
        ArgumentNullException.ThrowIfNull(request.RootScene);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.SetRootSceneAsync(request.SceneInstance, request.RootScene, cancellationToken);
        return new RootSceneSet(request.SceneInstance, request.RootScene);
    }

    public static async ValueTask<RootSceneCleared> ClearRootSceneAsync(
        RootSceneClearRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.SceneInstance);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.ClearRootSceneAsync(request.SceneInstance, cancellationToken);
        return new RootSceneCleared(request.SceneInstance);
    }

    public static async ValueTask<EntitySceneAttached> AttachEntityAsync(
        EntitySceneAttachRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Entity);
        ArgumentNullException.ThrowIfNull(request.Scene);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.AttachEntityToSceneAsync(request.Entity, request.Scene, cancellationToken);
        return new EntitySceneAttached(request.Entity, request.Scene);
    }

    public static async ValueTask<EntitySceneDetached> DetachEntityAsync(
        EntitySceneDetachRequested request,
        ISceneLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Entity);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.DetachEntityFromSceneAsync(request.Entity, cancellationToken);
        return new EntitySceneDetached(request.Entity);
    }
}
