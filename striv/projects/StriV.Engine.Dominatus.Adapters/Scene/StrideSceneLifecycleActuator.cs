using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Adapters;

public sealed class StrideSceneLifecycleActuator : ISceneLifecycleActuator
{
    public ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scene);
        return ValueTask.CompletedTask;
    }

    public ValueTask AttachEntityToSceneAsync(Entity entity, Scene scene, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        ArgumentNullException.ThrowIfNull(scene);

        entity.Scene = scene;
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);

        DetachFromScene(entity);
        return ValueTask.CompletedTask;
    }

    private static void DetachFromScene(Entity entity)
    {
        // Legacy Stride detach API: null scene means detach.
        // Contained here as a compatibility boundary until Stri-V introduces explicit detach APIs.
        entity.Scene = null!;
    }
}
