using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Adapters;

public sealed class StrideProcessorLifecycleActuator : IProcessorLifecycleActuator
{
    public ValueTask AddProcessorToSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(entityManager);

        entityManager.Processors.Add(processor);
        return ValueTask.CompletedTask;
    }

    public ValueTask RemoveProcessorFromSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(entityManager);

        entityManager.Processors.Remove(processor);
        return ValueTask.CompletedTask;
    }

    public ValueTask AddEntityToProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Current Stride API does not expose a safe public processor-entity attach surface without runtime rewiring.");

    public ValueTask RemoveEntityFromProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Current Stride API does not expose a safe public processor-entity detach surface without runtime rewiring.");
}
