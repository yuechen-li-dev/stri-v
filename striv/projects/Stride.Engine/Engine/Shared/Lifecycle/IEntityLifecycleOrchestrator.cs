using System.Threading;
using System.Threading.Tasks;

namespace Stride.Engine.Lifecycle;

public interface IEntityLifecycleOrchestrator
{
    ValueTask AttachSceneTransformAndProcessorAsync(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor,
        CancellationToken cancellationToken = default);

    ValueTask CleanupProcessorLifecycleAsync(
        EntityManager entityManager,
        Entity child,
        EntityProcessor processor,
        CancellationToken cancellationToken = default);

    ValueTask DetachTransformParentAsync(
        Entity child,
        CancellationToken cancellationToken = default);

    ValueTask DetachEntityFromSceneAsync(
        Entity entity,
        CancellationToken cancellationToken = default);

    ValueTask RunSceneTransformProcessorFullCycleAsync(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor,
        CancellationToken cancellationToken = default);
}
