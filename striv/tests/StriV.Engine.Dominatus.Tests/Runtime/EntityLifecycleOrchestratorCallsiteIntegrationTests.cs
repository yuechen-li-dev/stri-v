using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EntityLifecycleOrchestratorCallsiteIntegrationTests
{
    [Fact]
    public async Task EntityManager_RunEntityLifecycleFullCycle_WithDominatusOrchestrator_RunsFullCycle()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();
        var orchestrator = new DominatusEntityLifecycleOrchestrator();

        await entityManager.RunEntityLifecycleFullCycleAsync(orchestrator, scene, parent, child, processor);

        Assert.Null(parent.Scene);
        Assert.Null(child.Scene);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(processor, entityManager.Processors);
        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
        Assert.Same(child, Assert.Single(processor.RemovedEntities));
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
