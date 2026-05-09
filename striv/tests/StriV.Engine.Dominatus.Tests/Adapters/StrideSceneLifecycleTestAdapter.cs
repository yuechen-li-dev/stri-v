using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Tests.Adapters;

internal sealed class StrideSceneLifecycleTestAdapter : ISceneLifecycleActuator
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
        // Legacy Stride detach API: null scene means detach.
        // This null assignment is intentionally contained in the test adapter boundary.
        entity.Scene = null!;
    }
}
