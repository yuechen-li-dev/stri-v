using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;
using Dominatus.OptFlow;
using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Nodes;

public static class ProcessorLifecycleNode
{
    public static ProcessorSystemAddRequested RequestSystemAdd(EntityProcessor processor, EntityManager entityManager) => new(processor, entityManager);

    public static ProcessorSystemRemoveRequested RequestSystemRemove(EntityProcessor processor, EntityManager entityManager) => new(processor, entityManager);

    public static ProcessorEntityAddRequested RequestEntityAdd(EntityProcessor processor, Entity entity) => new(processor, entity);

    public static ProcessorEntityRemoveRequested RequestEntityRemove(EntityProcessor processor, Entity entity) => new(processor, entity);

    public static ValueTask<ProcessorSystemAdded> ExecuteSystemAddAsync(ProcessorSystemAddRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
        => ProcessorLifecycleTransition.AddProcessorToSystemAsync(request, actuator, cancellationToken);

    public static ValueTask<ProcessorSystemRemoved> ExecuteSystemRemoveAsync(ProcessorSystemRemoveRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
        => ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(request, actuator, cancellationToken);

    public static ValueTask<ProcessorEntityAdded> ExecuteEntityAddAsync(ProcessorEntityAddRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
        => ProcessorLifecycleTransition.AddEntityToProcessorAsync(request, actuator, cancellationToken);

    public static ValueTask<ProcessorEntityRemoved> ExecuteEntityRemoveAsync(ProcessorEntityRemoveRequested request, IProcessorLifecycleActuator actuator, CancellationToken cancellationToken = default)
        => ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(request, actuator, cancellationToken);

    public static IEnumerator<AiStep> Idle(AiCtx _)
    {
        while (true)
            yield return Ai.Wait(0.1f);
    }
}
