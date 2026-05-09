using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Transitions;

public static class SceneLifecycleTransition
{
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
