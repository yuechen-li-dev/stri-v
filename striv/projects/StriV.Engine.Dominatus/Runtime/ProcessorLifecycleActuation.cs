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

public sealed class ProcessorSystemRemoveActuationHandler(IProcessorLifecycleActuator actuator)
    : IActuationHandler<ProcessorSystemRemoveRequested>
{
    private readonly IProcessorLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        ProcessorSystemRemoveRequested command)
    {
        var completed = ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}

public sealed class ProcessorEntityRemoveActuationHandler(IProcessorLifecycleActuator actuator)
    : IActuationHandler<ProcessorEntityRemoveRequested>
{
    private readonly IProcessorLifecycleActuator _actuator = actuator ?? throw new ArgumentNullException(nameof(actuator));

    public ActuatorHost.HandlerResult Handle(
        ActuatorHost host,
        AiCtx ctx,
        ActuationId id,
        ProcessorEntityRemoveRequested command)
    {
        var completed = ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(command, _actuator, ctx.Cancel)
            .AsTask()
            .GetAwaiter()
            .GetResult();

        return ActuatorHost.HandlerResult.CompletedWithPayload(completed);
    }
}
