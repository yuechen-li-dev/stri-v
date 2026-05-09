using Stride.Engine;

namespace StriV.Engine.Dominatus.Runtime;

public sealed class DominatusEntityLifecycleOrchestrator : IEntityLifecycleOrchestrator
{
    private readonly StriVEngineLifecycleRunner runner;

    public DominatusEntityLifecycleOrchestrator()
        : this(new StriVEngineLifecycleRunner())
    {
    }

    public DominatusEntityLifecycleOrchestrator(StriVEngineLifecycleRunner runner)
    {
        this.runner = runner ?? throw new ArgumentNullException(nameof(runner));
    }

    public ValueTask AttachSceneTransformAndProcessorAsync(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor,
        CancellationToken cancellationToken = default)
        => runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor, cancellationToken);

    public ValueTask CleanupProcessorLifecycleAsync(
        EntityManager entityManager,
        Entity child,
        EntityProcessor processor,
        CancellationToken cancellationToken = default)
        => runner.CleanupProcessorLifecycleAsync(entityManager, child, processor, cancellationToken);

    public ValueTask DetachTransformParentAsync(Entity child, CancellationToken cancellationToken = default)
        => runner.DetachTransformParentAsync(child, cancellationToken);

    public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default)
        => runner.DetachEntityFromSceneAsync(entity, cancellationToken);

    public ValueTask RunSceneTransformProcessorFullCycleAsync(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor,
        CancellationToken cancellationToken = default)
        => runner.RunSceneTransformProcessorFullCycleAsync(scene, parent, child, entityManager, processor, cancellationToken);
}
