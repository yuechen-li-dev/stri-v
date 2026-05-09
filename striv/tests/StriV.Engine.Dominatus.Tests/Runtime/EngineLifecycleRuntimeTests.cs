using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;

using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EngineLifecycleRuntimeTests
{
    [Fact]
    public void DominatusRuntime_AttachSceneTransformAndProcessor_ComposesThroughSampleStyleNode()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        TickNode(
            () => EngineLifecycleDominatusNodes.AttachSceneTransformAndProcessor(scene, parent, child, entityManager, processor),
            host =>
            {
                host.Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()));
                host.Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));
                host.Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()));
                host.Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));
            });

        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Contains(parent, scene.Entities);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
        Assert.Same(entityManager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
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
