using Stride.Core;
using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class EntityManagerProcessorPolicyTests
{
    [Fact]
    public void EntityProcessor_AssociatedData_AddRemoveLifecycle_PassesNonNullDataWhenMatched()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: true);
        var processor = new AssociatedDataRecordingProcessor();
        manager.Processors.Add(processor);

        manager.AddEntityToProcessor(processor, entity);
        manager.RemoveEntityFromProcessor(processor, entity);

        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.NotNull(processor.LastAddedData);
        Assert.NotNull(processor.LastRemovedData);
        Assert.Equal(processor.LastAddedData, processor.LastRemovedData);
    }

    [Fact]
    public void EntityProcessor_AssociatedData_RemoveWithoutPriorAdd_DoesNotNullReference()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new AssociatedDataRecordingProcessor();
        manager.Processors.Add(processor);

        var exception = Record.Exception(() => manager.RemoveEntityFromProcessor(processor, entity));

        Assert.Null(exception);
        Assert.Equal(0, processor.RemovedCount);
    }

    private static SceneInstance CreateManagerWithEntity(out Entity entity, bool withComponent)
    {
        var manager = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        entity = new Entity("Entity");
        if (withComponent)
        {
            entity.Components.Add(new TestComponent());
        }

        manager.RootScene.Entities.Add(entity);
        return manager;
    }

    private sealed class TestComponent : EntityComponent;

    private sealed class AssociatedDataRecordingProcessor : EntityProcessor<TestComponent, string>
    {
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public string? LastAddedData { get; private set; }
        public string? LastRemovedData { get; private set; }

        protected override string GenerateComponentData(Entity entity, TestComponent component) => $"{entity.Name}:{component.GetType().Name}";

        protected override void OnEntityComponentAdding(Entity entity, TestComponent component, string data)
        {
            AddedCount++;
            LastAddedData = data;
        }

        protected override void OnEntityComponentRemoved(Entity entity, TestComponent component, string data)
        {
            RemovedCount++;
            LastRemovedData = data;
        }
    }
}
