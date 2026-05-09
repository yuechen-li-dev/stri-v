using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Adapters;

public sealed class StrideTransformLifecycleActuator : ITransformLifecycleActuator
{
    public ValueTask AttachParentAsync(Entity child, Entity parent, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(parent);

        child.Transform.Parent = parent.Transform;
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachParentAsync(Entity child, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(child);

        DetachFromParent(child);
        return ValueTask.CompletedTask;
    }

    private static void DetachFromParent(Entity child)
    {
        // Legacy Stride detach API: null parent means detach.
        // Contained here as a compatibility boundary until Stri-V introduces explicit detach APIs.
        child.Transform.Parent = null!;
    }
}
