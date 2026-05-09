using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Runtime;
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

        TickNode(
            () => ProcessorLifecycleDominatusNodes.AddProcessorToSystem(processor, manager),
            host => host.Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator())));

        Assert.Same(manager, processor.EntityManager);
        Assert.Same(processor, manager.GetProcessor<TransformProcessor>());
    }

    [Fact]
    public void DominatusRuntime_AddEntityToProcessor_ActsThroughProductionAdapter()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);

        TickNode(
            () => ProcessorLifecycleDominatusNodes.AddEntityToProcessor(processor, entity),
            host => host.Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator())));

        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));
    }

    [Fact]
    public void DominatusRuntime_AddProcessorAndEntity_ComposesThroughProductionAdapter()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();

        TickNode(
            () => ProcessorLifecycleDominatusNodes.AddProcessorAndEntity(processor, manager, entity),
            host =>
            {
                host.Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()));
                host.Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));
            });

        Assert.Same(manager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));
    }

    private static void TickNode(Func<IEnumerator<AiStep>> nodeFactory, Action<ActuatorHost> registerHandlers)
    {
        var graph = new HfsmGraph { Root = new StateId("Root") };
        graph.Add(new HfsmStateDef { Id = "Root", Node = _ => nodeFactory() });

        var host = new ActuatorHost();
        registerHandlers(host);

        var world = new AiWorld(host);
        var agent = new AiAgent(new HfsmInstance(graph));
        world.Add(agent);
        agent.Brain.Initialize(world, agent);

        world.Tick(0.016f);
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
