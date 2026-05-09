using Dominatus.Core.Runtime;

using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Runtime;

public sealed class TransformParentAttachActuationHandler(ITransformLifecycleActuator actuator)
    : IActuationHandler<TransformParentAttachRequested>
{
    private readonly ITransformLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        TransformParentAttachRequested command)
    {
        TransformLifecycleTransition.AttachParentAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(new TransformParentAttached(command.Child, command.Parent));
    }
}

public sealed class TransformParentDetachActuationHandler(ITransformLifecycleActuator actuator)
    : IActuationHandler<TransformParentDetachRequested>
{
    private readonly ITransformLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        TransformParentDetachRequested command)
    {
        var completed = TransformLifecycleTransition
            .DetachParentAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}
