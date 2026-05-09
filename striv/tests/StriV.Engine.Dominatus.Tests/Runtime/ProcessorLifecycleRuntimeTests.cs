using Dominatus.Core.Nodes;

using Stride.Core;
using Stride.Engine;
using Stride.Engine.Processors;

using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class ProcessorLifecycleRuntimeTests
{
    [Fact]
    public void DominatusRuntime_AddProcessorToSystem_ActsThroughProductionAdapter()
    {
        var manager = new SceneInstance(new ServiceRegistry());
        var processor = new TransformProcessor();

        var harness = new DominatusRuntimeTestHarness()
            .Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Root",
            _ => ProcessorLifecycleDominatusNodes.AddProcessorToSystem(processor, manager));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(manager, processor.EntityManager);
        Assert.Same(processor, manager.GetProcessor<TransformProcessor>());
    }

    [Fact]
    public void DominatusRuntime_AddEntityToProcessor_ActsThroughProductionAdapter()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);

        var harness = new DominatusRuntimeTestHarness()
            .Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Root",
            _ => ProcessorLifecycleDominatusNodes.AddEntityToProcessor(processor, entity));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));
    }

    [Fact]
    public void DominatusRuntime_AddProcessorAndEntity_ComposesThroughProductionAdapter()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();

        var harness = new DominatusRuntimeTestHarness()
            .Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()))
            .Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Root",
            _ => ProcessorLifecycleDominatusNodes.AddProcessorAndEntity(processor, manager, entity));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(manager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));
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
        public List<Entity> AddedEntities { get; } = [];

        protected override void OnEntityComponentAdding(Entity entity, TestComponent component, TestComponent data)
        {
            AddedCount++;
            AddedEntities.Add(entity);
        }
    }
}
