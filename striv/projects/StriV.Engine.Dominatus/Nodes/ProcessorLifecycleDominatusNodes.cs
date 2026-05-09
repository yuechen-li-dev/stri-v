using Dominatus.Core.Nodes;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Nodes;

public static class ProcessorLifecycleDominatusNodes
{
    public static IEnumerator<AiStep> AddProcessorToSystem(EntityProcessor processor, EntityManager entityManager)
    {
        yield return Ai.Act(new ProcessorSystemAddRequested(processor, entityManager));
    }

    public static IEnumerator<AiStep> AddEntityToProcessor(EntityProcessor processor, Entity entity)
    {
        yield return Ai.Act(new ProcessorEntityAddRequested(processor, entity));
    }

    public static IEnumerator<AiStep> AddProcessorAndEntity(EntityProcessor processor, EntityManager entityManager, Entity entity)
    {
        yield return Ai.Act(new ProcessorSystemAddRequested(processor, entityManager));
        yield return Ai.Act(new ProcessorEntityAddRequested(processor, entity));
    }

    public static IEnumerator<AiStep> RemoveEntityFromProcessor(EntityProcessor processor, Entity entity)
    {
        yield return Ai.Act(new ProcessorEntityRemoveRequested(processor, entity));
    }

    public static IEnumerator<AiStep> RemoveProcessorFromSystem(EntityProcessor processor, EntityManager entityManager)
    {
        yield return Ai.Act(new ProcessorSystemRemoveRequested(processor, entityManager));
    }

    public static IEnumerator<AiStep> RemoveProcessorAndEntity(EntityProcessor processor, EntityManager entityManager, Entity entity)
    {
        ArgumentNullException.ThrowIfNull(processor);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(entity);

        yield return Ai.Act(new ProcessorEntityRemoveRequested(processor, entity));
        yield return Ai.Act(new ProcessorSystemRemoveRequested(processor, entityManager));
    }
}
