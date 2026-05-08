using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Nodes;

public static class EntityAttachmentNode
{
    public static TransformParentAttachRequested RequestTransformAttach(Entity child, Entity parent) => new(child, parent);

    public static TransformParentDetachRequested RequestTransformDetach(Entity child) => new(child);

    public static ValueTask<TransformParentAttached> ExecuteAttachAsync(
        TransformParentAttachRequested request,
        ITransformLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => TransformLifecycleTransition.AttachParentAsync(request, actuator, cancellationToken);

    public static ValueTask<TransformParentDetached> ExecuteDetachAsync(
        TransformParentDetachRequested request,
        ITransformLifecycleActuator actuator,
        CancellationToken cancellationToken = default)
        => TransformLifecycleTransition.DetachParentAsync(request, actuator, cancellationToken);

    public static IEnumerator<AiStep> Idle(AiCtx _)
    {
        while (true)
            yield return Ai.Wait(0.1f);
    }
}
