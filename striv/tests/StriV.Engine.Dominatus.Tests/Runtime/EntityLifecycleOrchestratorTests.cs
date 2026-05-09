using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EntityLifecycleOrchestratorTests
{
    [Fact]
    public async Task DominatusEntityLifecycleOrchestrator_AttachSceneTransformAndProcessor_DelegatesToRunnerPath()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        IEntityLifecycleOrchestrator orchestrator = new DominatusEntityLifecycleOrchestrator();

        await orchestrator.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor);

        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Same(entityManager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
    }

    [Fact]
    public async Task DominatusEntityLifecycleOrchestrator_CleanupProcessorLifecycle_DelegatesToRunnerPath()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        IEntityLifecycleOrchestrator orchestrator = new DominatusEntityLifecycleOrchestrator();
        await orchestrator.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor);

        await orchestrator.CleanupProcessorLifecycleAsync(entityManager, child, processor);

        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(child, Assert.Single(processor.RemovedEntities));
        Assert.DoesNotContain(processor, entityManager.Processors);
        Assert.Same(scene, child.Scene);
        Assert.Same(parent.Transform, child.Transform.Parent);
    }

    [Fact]
    public async Task DominatusEntityLifecycleOrchestrator_RunFullCycle_DelegatesToRunnerPath()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        IEntityLifecycleOrchestrator orchestrator = new DominatusEntityLifecycleOrchestrator();

        await orchestrator.RunSceneTransformProcessorFullCycleAsync(scene, parent, child, entityManager, processor);

        Assert.Null(parent.Scene);
        Assert.Null(child.Scene);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(processor, entityManager.Processors);
        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
    }

    [Fact]
    public void DominatusEntityLifecycleOrchestrator_Constructor_RejectsNullRunner()
    {
        Assert.Throws<ArgumentNullException>(() => new DominatusEntityLifecycleOrchestrator(null!));
    }

    private sealed class TestComponent : EntityComponent;

    private sealed class RecordingProcessor : EntityProcessor<TestComponent>
    {
        public int AddedCount { get; private set; }
        public List<Entity> AddedEntities { get; } = [];
        public int RemovedCount { get; private set; }
        public List<Entity> RemovedEntities { get; } = [];

        protected override void OnEntityComponentAdding(Entity entity, TestComponent component, TestComponent data)
        {
            AddedCount++;
            AddedEntities.Add(entity);
        }

        protected override void OnEntityComponentRemoved(Entity entity, TestComponent component, TestComponent data)
        {
            RemovedCount++;
            RemovedEntities.Add(entity);
        }
    }
}
