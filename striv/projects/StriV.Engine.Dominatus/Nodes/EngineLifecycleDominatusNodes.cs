using Dominatus.Core.Nodes;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Nodes;

public static class EngineLifecycleDominatusNodes
{
    public static IEnumerator<AiStep> AttachSceneTransformAndProcessor(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(processor);

        yield return Ai.Act(new EntitySceneAttachRequested(parent, scene));
        yield return Ai.Act(new EntitySceneAttachRequested(child, scene));
        yield return Ai.Act(new TransformParentAttachRequested(child, parent));
        yield return Ai.Act(new ProcessorSystemAddRequested(processor, entityManager));
        yield return Ai.Act(new ProcessorEntityAddRequested(processor, child));
    }

    public static IEnumerator<AiStep> AttachThenProcessorCleanupSceneTransformAndProcessor(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(processor);

        yield return Ai.Act(new EntitySceneAttachRequested(parent, scene));
        yield return Ai.Act(new EntitySceneAttachRequested(child, scene));
        yield return Ai.Act(new TransformParentAttachRequested(child, parent));
        yield return Ai.Act(new ProcessorSystemAddRequested(processor, entityManager));
        yield return Ai.Act(new ProcessorEntityAddRequested(processor, child));

        yield return Ai.Act(new ProcessorEntityRemoveRequested(processor, child));
        yield return Ai.Act(new ProcessorSystemRemoveRequested(processor, entityManager));
    }

    public static IEnumerator<AiStep> RunSceneTransformProcessorFullCycle(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(processor);

        yield return Ai.Act(new EntitySceneAttachRequested(parent, scene));
        yield return Ai.Act(new EntitySceneAttachRequested(child, scene));
        yield return Ai.Act(new TransformParentAttachRequested(child, parent));
        yield return Ai.Act(new ProcessorSystemAddRequested(processor, entityManager));
        yield return Ai.Act(new ProcessorEntityAddRequested(processor, child));

        yield return Ai.Act(new ProcessorEntityRemoveRequested(processor, child));
        yield return Ai.Act(new ProcessorSystemRemoveRequested(processor, entityManager));
        yield return Ai.Act(new TransformParentDetachRequested(child));
        yield return Ai.Act(new EntitySceneDetachRequested(child));
        yield return Ai.Act(new EntitySceneDetachRequested(parent));
    }
}
