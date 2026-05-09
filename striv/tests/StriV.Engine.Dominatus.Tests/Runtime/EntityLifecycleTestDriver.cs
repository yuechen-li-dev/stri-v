using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Runtime;

namespace StriV.Engine.Dominatus.Tests.Runtime;

internal sealed class EntityLifecycleFixture
{
    public Scene Scene { get; } = new();
    public Entity Parent { get; } = new("Parent");
    public Entity Child { get; } = new("Child");
    public SceneInstance EntityManager { get; } = new(new ServiceRegistry());
    public RecordingProcessor Processor { get; } = new();

    public EntityLifecycleFixture()
    {
        Child.Components.Add(new TestComponent());
    }
}

internal static class EntityLifecycleTestDriver
{
    public static ValueTask RunDominatusFullCycleAsync(EntityLifecycleFixture fixture)
    {
        var orchestrator = new DominatusEntityLifecycleOrchestrator();
        return fixture.EntityManager.RunEntityLifecycleFullCycleAsync(orchestrator, fixture.Scene, fixture.Parent, fixture.Child, fixture.Processor);
    }

    // Legacy direct path is retained as a control baseline only.
    public static void RunLegacyDirectFullCycle(EntityLifecycleFixture fixture)
    {
        fixture.Parent.Scene = fixture.Scene;
        fixture.Child.Scene = fixture.Scene;
        fixture.Child.Transform.Parent = fixture.Parent.Transform;

        fixture.EntityManager.Processors.Add(fixture.Processor);
        fixture.EntityManager.AddEntityToProcessor(fixture.Processor, fixture.Child);

        fixture.EntityManager.RemoveEntityFromProcessor(fixture.Processor, fixture.Child);
        fixture.EntityManager.Processors.Remove(fixture.Processor);

        fixture.Child.Transform.Parent = null!;
        fixture.Child.Scene = null!;
        fixture.Parent.Scene = null!;
    }

    public static LifecycleSnapshot CaptureSnapshot(EntityLifecycleFixture fixture)
        => new(
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

internal sealed record LifecycleSnapshot(
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

internal sealed class TestComponent : EntityComponent;

internal sealed class RecordingProcessor : EntityProcessor<TestComponent>
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
