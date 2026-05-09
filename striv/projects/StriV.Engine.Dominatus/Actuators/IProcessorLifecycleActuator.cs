using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface IProcessorLifecycleActuator
{
    ValueTask AddProcessorToSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default);
    ValueTask RemoveProcessorFromSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default);
    ValueTask AddEntityToProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default);
    ValueTask RemoveEntityFromProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default);
}
