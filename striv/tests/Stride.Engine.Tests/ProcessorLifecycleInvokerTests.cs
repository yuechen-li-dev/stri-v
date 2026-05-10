using Stride.Core;
using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class ProcessorLifecycleInvokerTests
{
    [Fact]
    public void EntityManager_AddEntityToProcessor_InvokesAddOnce_WhenEntityMatches()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);

        manager.AddEntityToProcessor(processor, entity);

        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));
    }

    [Fact]
    public void EntityManager_RemoveEntityFromProcessor_InvokesRemoveOnce_WhenEntityWasMatched()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);
        manager.AddEntityToProcessor(processor, entity);

        manager.RemoveEntityFromProcessor(processor, entity);

        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(entity, Assert.Single(processor.RemovedEntities));
    }

    [Fact]
    public void EntityManager_AddEntityToProcessor_RespectsComponentMatching()
    {
        var manager = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        var entity = new Entity("NoMatch");
        manager.RootScene.Entities.Add(entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);

        manager.AddEntityToProcessor(processor, entity);

        Assert.Equal(0, processor.AddedCount);
    }

    [Fact]
    public void EntityManager_AddEntityToProcessor_RequiresProcessorBoundToSameManager()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var foreign = new RecordingProcessor();

        var ex = Assert.Throws<InvalidOperationException>(() => manager.AddEntityToProcessor(foreign, entity));
        Assert.Contains("not attached", ex.Message);
    }

    [Fact]
    public void EntityManager_RemoveEntityFromProcessor_RequiresProcessorBoundToSameManager()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var foreign = new RecordingProcessor();

        var ex = Assert.Throws<InvalidOperationException>(() => manager.RemoveEntityFromProcessor(foreign, entity));
        Assert.Contains("not attached", ex.Message);
    }

    private static SceneInstance CreateManagerWithEntity(out Entity entity)
    {
        var manager = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        entity = new Entity("Entity");
        entity.Components.Add(new TestComponent());
        manager.RootScene.Entities.Add(entity);
        return manager;
    }

    private sealed class TestComponent : EntityComponent;

    private sealed class RecordingProcessor : EntityProcessor<TestComponent>
    {
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public List<Entity> AddedEntities { get; } = [];
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
