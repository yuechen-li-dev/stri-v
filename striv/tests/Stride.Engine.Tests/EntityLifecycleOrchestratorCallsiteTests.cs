using Stride.Core;
using Stride.Engine.Lifecycle;

using Xunit;

namespace Stride.Engine.Tests;

public sealed class EntityLifecycleOrchestratorCallsiteTests
{
    [Fact]
    public async Task EntityManager_RunEntityLifecycleFullCycle_DelegatesToOrchestratorWithThisManager()
    {
        var entityManager = new SceneInstance(new ServiceRegistry());
        var scene = new Scene();
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var processor = new RecordingProcessor();
        var orchestrator = new RecordingOrchestrator();

        await entityManager.RunEntityLifecycleFullCycleAsync(orchestrator, scene, parent, child, processor);

        Assert.Equal(1, orchestrator.CallCount);
        Assert.Same(entityManager, orchestrator.EntityManager);
        Assert.Same(scene, orchestrator.Scene);
        Assert.Same(parent, orchestrator.Parent);
        Assert.Same(child, orchestrator.Child);
        Assert.Same(processor, orchestrator.Processor);
    }

    [Fact]
    public async Task EntityManager_RunEntityLifecycleFullCycle_RejectsNullOrchestrator()
    {
        var entityManager = new SceneInstance(new ServiceRegistry());

        await Assert.ThrowsAsync<ArgumentNullException>(
            () => entityManager.RunEntityLifecycleFullCycleAsync(null!, new Scene(), new Entity("Parent"), new Entity("Child"), new RecordingProcessor()).AsTask());
    }

    private sealed class RecordingOrchestrator : IEntityLifecycleOrchestrator
    {
        public int CallCount { get; private set; }
        public Scene? Scene { get; private set; }
        public Entity? Parent { get; private set; }
        public Entity? Child { get; private set; }
        public EntityManager? EntityManager { get; private set; }
        public EntityProcessor? Processor { get; private set; }

        public ValueTask AttachSceneTransformAndProcessorAsync(Scene scene, Entity parent, Entity child, EntityManager entityManager, EntityProcessor processor, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask CleanupProcessorLifecycleAsync(EntityManager entityManager, Entity child, EntityProcessor processor, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask DetachTransformParentAsync(Entity child, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default)
            => ValueTask.CompletedTask;

        public ValueTask RunSceneTransformProcessorFullCycleAsync(Scene scene, Entity parent, Entity child, EntityManager entityManager, EntityProcessor processor, CancellationToken cancellationToken = default)
        {
            CallCount++;
            Scene = scene;
            Parent = parent;
            Child = child;
            EntityManager = entityManager;
            Processor = processor;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class TestComponent : EntityComponent;

    private sealed class RecordingProcessor : EntityProcessor<TestComponent>
    {
    }
}
