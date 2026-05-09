using Dominatus.Core.Runtime;

using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Runtime;

public sealed class EntitySceneAttachActuationHandler(ISceneLifecycleActuator actuator)
    : IActuationHandler<EntitySceneAttachRequested>
{
    private readonly ISceneLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        EntitySceneAttachRequested command)
    {
        SceneLifecycleTransition.AttachEntityAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(new EntitySceneAttached(command.Entity, command.Scene));
    }
}

public sealed class EntitySceneDetachActuationHandler(ISceneLifecycleActuator actuator)
    : IActuationHandler<EntitySceneDetachRequested>
{
    private readonly ISceneLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        EntitySceneDetachRequested command)
    {
        var completed = SceneLifecycleTransition
            .DetachEntityAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}
