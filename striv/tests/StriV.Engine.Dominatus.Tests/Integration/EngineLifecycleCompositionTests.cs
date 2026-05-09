using Stride.Core;
using Stride.Engine;
using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests.Integration;

public sealed class EngineLifecycleCompositionTests
{
    [Fact]
    public async Task EngineLifecycleComposition_TransformSceneProcessorTransitions_ComposeThroughProductionAdapters()
    {
        var scene = new Scene();
        var manager = new SceneInstance(new ServiceRegistry()) { RootScene = scene };
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        var transformActuator = new StrideTransformLifecycleActuator();
        var sceneActuator = new StrideSceneLifecycleActuator();
        var processorActuator = new StrideProcessorLifecycleActuator();

        // Stride lifecycle constraint: entity scene attach must happen before setting a non-null transform parent.
        var sceneAttached = await SceneLifecycleTransition.AttachEntityAsync(new EntitySceneAttachRequested(parent, scene), sceneActuator);
        var childSceneAttached = await SceneLifecycleTransition.AttachEntityAsync(new EntitySceneAttachRequested(child, scene), sceneActuator);

        Assert.Same(parent, sceneAttached.Entity);
        Assert.Same(scene, sceneAttached.Scene);
        Assert.Same(child, childSceneAttached.Entity);
        Assert.Same(scene, childSceneAttached.Scene);
        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Contains(parent, scene.Entities);
        Assert.Contains(child, scene.Entities);

        var parentAttached = await TransformLifecycleTransition.AttachParentAsync(new TransformParentAttachRequested(child, parent), transformActuator);

        Assert.Same(child, parentAttached.Child);
        Assert.Same(parent, parentAttached.Parent);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);

        var processorSystemAdded = await ProcessorLifecycleTransition.AddProcessorToSystemAsync(new ProcessorSystemAddRequested(processor, manager), processorActuator);

        Assert.Same(processor, processorSystemAdded.Processor);
        Assert.Same(manager, processorSystemAdded.EntityManager);
        Assert.Same(manager, processor.EntityManager);

        var processorEntityAdded = await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, child), processorActuator);

        Assert.Same(processor, processorEntityAdded.Processor);
        Assert.Same(child, processorEntityAdded.Entity);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
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
