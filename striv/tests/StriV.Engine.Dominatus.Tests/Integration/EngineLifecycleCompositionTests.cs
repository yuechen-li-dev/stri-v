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
    public async Task RootSceneComposition_RemoveProcessorAndClearRootScene_ComposeThroughProductionAdapters()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var entity = new Entity("RootEntity");
        entity.Components.Add(new TestComponent());
        rootScene.Entities.Add(entity);
        var processor = new RecordingProcessor();

        var sceneActuator = new StrideSceneLifecycleActuator();
        var processorActuator = new StrideProcessorLifecycleActuator();

        var rootSet = await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), sceneActuator);

        Assert.Same(sceneInstance, rootSet.SceneInstance);
        Assert.Same(rootScene, rootSet.RootScene);
        Assert.Same(rootScene, sceneInstance.RootScene);
        Assert.Same(sceneInstance, entity.EntityManager);

        var processorSystemAdded = await ProcessorLifecycleTransition.AddProcessorToSystemAsync(new ProcessorSystemAddRequested(processor, sceneInstance), processorActuator);

        Assert.Same(processor, processorSystemAdded.Processor);
        Assert.Same(sceneInstance, processorSystemAdded.EntityManager);
        Assert.Same(sceneInstance, processor.EntityManager);

        var processorEntityAdded = await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), processorActuator);

        Assert.Same(processor, processorEntityAdded.Processor);
        Assert.Same(entity, processorEntityAdded.Entity);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));

        // Explicit cleanup policy: remove processor entity membership first, then processor system membership, then clear root scene.
        var processorEntityRemoved = await ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(new ProcessorEntityRemoveRequested(processor, entity), processorActuator);

        Assert.Same(processor, processorEntityRemoved.Processor);
        Assert.Same(entity, processorEntityRemoved.Entity);
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(entity, Assert.Single(processor.RemovedEntities));

        var processorSystemRemoved = await ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(new ProcessorSystemRemoveRequested(processor, sceneInstance), processorActuator);

        Assert.Same(processor, processorSystemRemoved.Processor);
        Assert.Same(sceneInstance, processorSystemRemoved.EntityManager);
        Assert.Null(processor.EntityManager);
        Assert.DoesNotContain(processor, sceneInstance.Processors);

        var rootCleared = await SceneLifecycleTransition.ClearRootSceneAsync(new RootSceneClearRequested(sceneInstance), sceneActuator);

        Assert.Same(sceneInstance, rootCleared.SceneInstance);
        Assert.Null(sceneInstance.RootScene);
        Assert.Null(entity.EntityManager);
    }

    [Fact]
    public async Task RootSceneCleanupPolicy_ClearRootScene_ImplicitlyRemovesProcessorEntityMembership()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var entity = new Entity("RootEntity");
        entity.Components.Add(new TestComponent());
        rootScene.Entities.Add(entity);
        var processor = new RecordingProcessor();

        var sceneActuator = new StrideSceneLifecycleActuator();
        var processorActuator = new StrideProcessorLifecycleActuator();

        var rootSet = await SceneLifecycleTransition.SetRootSceneAsync(new RootSceneSetRequested(sceneInstance, rootScene), sceneActuator);

        Assert.Same(sceneInstance, rootSet.SceneInstance);
        Assert.Same(rootScene, rootSet.RootScene);
        Assert.Same(rootScene, sceneInstance.RootScene);
        Assert.Same(sceneInstance, entity.EntityManager);

        var processorSystemAdded = await ProcessorLifecycleTransition.AddProcessorToSystemAsync(new ProcessorSystemAddRequested(processor, sceneInstance), processorActuator);

        Assert.Same(processor, processorSystemAdded.Processor);
        Assert.Same(sceneInstance, processorSystemAdded.EntityManager);
        Assert.Same(sceneInstance, processor.EntityManager);

        var processorEntityAdded = await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), processorActuator);

        Assert.Same(processor, processorEntityAdded.Processor);
        Assert.Same(entity, processorEntityAdded.Entity);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(entity, Assert.Single(processor.AddedEntities));

        var rootCleared = await SceneLifecycleTransition.ClearRootSceneAsync(new RootSceneClearRequested(sceneInstance), sceneActuator);

        Assert.Same(sceneInstance, rootCleared.SceneInstance);
        Assert.Null(sceneInstance.RootScene);
        Assert.Null(entity.EntityManager);

        // Observed Stride behavior: clearing root scene removes existing processor/entity matches.
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(entity, Assert.Single(processor.RemovedEntities));
    }

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
