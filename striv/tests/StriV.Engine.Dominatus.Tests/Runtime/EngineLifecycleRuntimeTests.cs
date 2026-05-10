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

        var harness = new DominatusRuntimeTestHarness()
            .Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()))
            .Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()))
            .Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()))
            .Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Root",
            _ => EngineLifecycleDominatusNodes.AttachSceneTransformAndProcessor(scene, parent, child, entityManager, processor));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Contains(parent, scene.Entities);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
        Assert.Same(entityManager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
    }

    [Fact]
    public void DominatusRuntime_ComposedLifecycle_AddThenProcessorCleanup_ComposesThroughSampleStyleNode()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        var harness = new DominatusRuntimeTestHarness()
            .Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()))
            .Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()))
            .Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()))
            .Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()))
            .Register(new ProcessorEntityRemoveActuationHandler(new StrideProcessorLifecycleActuator()))
            .Register(new ProcessorSystemRemoveActuationHandler(new StrideProcessorLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Root",
            _ => EngineLifecycleDominatusNodes.AttachThenProcessorCleanupSceneTransformAndProcessor(scene, parent, child, entityManager, processor));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(child, Assert.Single(processor.RemovedEntities));
        Assert.False(processor.IsAttached);
        Assert.Throws<InvalidOperationException>(() => _ = processor.EntityManager);
        Assert.DoesNotContain(processor, entityManager.Processors);
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
