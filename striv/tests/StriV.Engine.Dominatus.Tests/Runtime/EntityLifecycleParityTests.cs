using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EntityLifecycleParityTests
{
    [Fact]
    public async Task EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot()
    {
        var legacy = new LifecycleFixture();
        var orchestrated = new LifecycleFixture();

        RunLegacyFullCycle(legacy);
        await RunOrchestratedFullCycleAsync(orchestrated);

        var legacySnapshot = Capture(legacy);
        var orchestratedSnapshot = Capture(orchestrated);

        Assert.Equal(legacySnapshot, orchestratedSnapshot);
    }

    private static void RunLegacyFullCycle(LifecycleFixture fixture)
    {
        fixture.Parent.Scene = fixture.Scene;
        fixture.Child.Scene = fixture.Scene;
        fixture.Child.Transform.Parent = fixture.Parent.Transform;

        fixture.EntityManager.Processors.Add(fixture.Processor);
        fixture.EntityManager.AddEntityToProcessor(fixture.Processor, fixture.Child);

        fixture.EntityManager.RemoveEntityFromProcessor(fixture.Processor, fixture.Child);
        fixture.EntityManager.Processors.Remove(fixture.Processor);

        // Legacy direct path: current Stride detach API uses null.
        fixture.Child.Transform.Parent = null!;
        // Legacy direct path: current Stride detach API uses null.
        fixture.Child.Scene = null!;
        // Legacy direct path: current Stride detach API uses null.
        fixture.Parent.Scene = null!;
    }

    private static async ValueTask RunOrchestratedFullCycleAsync(LifecycleFixture fixture)
    {
        var orchestrator = new DominatusEntityLifecycleOrchestrator();
        await fixture.EntityManager.RunEntityLifecycleFullCycleAsync(orchestrator, fixture.Scene, fixture.Parent, fixture.Child, fixture.Processor);
    }

    private static LifecycleSnapshot Capture(LifecycleFixture fixture)
    {
        return new LifecycleSnapshot(
            ParentSceneDetached: fixture.Parent.Scene is null,
            ChildSceneDetached: fixture.Child.Scene is null,
            ChildTransformDetached: fixture.Child.Transform.Parent is null,
            ParentChildrenDoesNotContainChild: !fixture.Parent.Transform.Children.Contains(fixture.Child.Transform),
            ProcessorDetached: fixture.Processor.EntityManager is null,
            ManagerDoesNotContainProcessor: !fixture.EntityManager.Processors.Contains(fixture.Processor),
            ProcessorAddedCount: fixture.Processor.AddedCount,
            ProcessorRemovedCount: fixture.Processor.RemovedCount,
            AddedEntityName: fixture.Processor.AddedEntities.SingleOrDefault()?.Name,
            RemovedEntityName: fixture.Processor.RemovedEntities.SingleOrDefault()?.Name);
    }

    private sealed class LifecycleFixture
    {
        public Scene Scene { get; } = new();
        public Entity Parent { get; } = new("Parent");
        public Entity Child { get; } = new("Child");
        public SceneInstance EntityManager { get; } = new(new ServiceRegistry());
        public RecordingProcessor Processor { get; } = new();

        public LifecycleFixture()
        {
            Child.Components.Add(new TestComponent());
        }
    }

    private sealed record LifecycleSnapshot(
        bool ParentSceneDetached,
        bool ChildSceneDetached,
        bool ChildTransformDetached,
        bool ParentChildrenDoesNotContainChild,
        bool ProcessorDetached,
        bool ManagerDoesNotContainProcessor,
        int ProcessorAddedCount,
        int ProcessorRemovedCount,
        string? AddedEntityName,
        string? RemovedEntityName);

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
