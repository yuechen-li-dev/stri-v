using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Transitions;

public static class ProcessorLifecycleTransition
{
    public static async ValueTask<ProcessorSystemAdded> AddProcessorToSystemAsync(ProcessorSystemAddRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Processor);
        ArgumentNullException.ThrowIfNull(request.EntityManager);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.AddProcessorToSystemAsync(request.Processor, request.EntityManager, cancellationToken);
        return new ProcessorSystemAdded(request.Processor, request.EntityManager);
    }

    public static async ValueTask<ProcessorSystemRemoved> RemoveProcessorFromSystemAsync(ProcessorSystemRemoveRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Processor);
        ArgumentNullException.ThrowIfNull(request.EntityManager);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.RemoveProcessorFromSystemAsync(request.Processor, request.EntityManager, cancellationToken);
        return new ProcessorSystemRemoved(request.Processor, request.EntityManager);
    }

    public static async ValueTask<ProcessorEntityAdded> AddEntityToProcessorAsync(ProcessorEntityAddRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Processor);
        ArgumentNullException.ThrowIfNull(request.Entity);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.AddEntityToProcessorAsync(request.Processor, request.Entity, cancellationToken);
        return new ProcessorEntityAdded(request.Processor, request.Entity);
    }

    public static async ValueTask<ProcessorEntityRemoved> RemoveEntityFromProcessorAsync(ProcessorEntityRemoveRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Processor);
        ArgumentNullException.ThrowIfNull(request.Entity);
        ArgumentNullException.ThrowIfNull(actuator);

        await actuator.RemoveEntityFromProcessorAsync(request.Processor, request.Entity, cancellationToken);
        return new ProcessorEntityRemoved(request.Processor, request.Entity);
    }
}
