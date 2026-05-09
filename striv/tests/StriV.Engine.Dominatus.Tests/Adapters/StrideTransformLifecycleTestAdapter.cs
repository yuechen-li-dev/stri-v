using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Tests.Adapters;

internal sealed class StrideTransformLifecycleTestAdapter : ITransformLifecycleActuator
{
    public int AttachCalls { get; private set; }
    public int DetachCalls { get; private set; }

    public ValueTask AttachParentAsync(Entity child, Entity parent, CancellationToken cancellationToken = default)
    {
        AttachCalls++;
        child.Transform.Parent = parent.Transform;
        return ValueTask.CompletedTask;
    }

    public ValueTask DetachParentAsync(Entity child, CancellationToken cancellationToken = default)
    {
        DetachCalls++;
        DetachFromParent(child);
        return ValueTask.CompletedTask;
    }

    private static void DetachFromParent(Entity child)
    {
        // Legacy Stride detach API: null parent means detach.
        // This null assignment is intentionally contained in the test adapter boundary.
        child.Transform.Parent = null!;
    }
}
