using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Transitions;

public static class TransformLifecycleTransition
{
    public static async ValueTask<TransformParentAttached> AttachParentAsync(
        TransformParentAttachRequested request,
        ITransformLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.AttachParentAsync(request.Child, request.Parent, cancellationToken);
        return new TransformParentAttached(request.Child, request.Parent);
    }

    public static async ValueTask<TransformParentDetached> DetachParentAsync(
        TransformParentDetachRequested request,
        ITransformLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.DetachParentAsync(request.Child, cancellationToken);
        return new TransformParentDetached(request.Child);
    }
}
