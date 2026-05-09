using Dominatus.Core.Runtime;

using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Transitions;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Runtime;

public sealed class ProcessorSystemAddActuationHandler(IProcessorLifecycleActuator actuator)
    : IActuationHandler<ProcessorSystemAddRequested>
{
    private readonly IProcessorLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        ProcessorSystemAddRequested command)
    {
        var completed = ProcessorLifecycleTransition.AddProcessorToSystemAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}

public sealed class ProcessorEntityAddActuationHandler(IProcessorLifecycleActuator actuator)
    : IActuationHandler<ProcessorEntityAddRequested>
{
    private readonly IProcessorLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        ProcessorEntityAddRequested command)
    {
        var completed = ProcessorLifecycleTransition.AddEntityToProcessorAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}
