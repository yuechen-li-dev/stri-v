using Stride.Core;
using Stride.Engine;
using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class ProductionAdapterTests
{
    [Fact]
    public async Task StrideTransformLifecycleActuator_AttachParent_UsesStrideParenting()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleActuator();

        await actuator.AttachParentAsync(child, parent);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideTransformLifecycleActuator_DetachParent_UsesStrideDetach()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleActuator();

        await actuator.AttachParentAsync(child, parent);
        await actuator.DetachParentAsync(child);

        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideSceneLifecycleActuator_AttachEntity_UsesStrideSceneMembership()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new StrideSceneLifecycleActuator();

        await actuator.AttachEntityToSceneAsync(entity, scene);

        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public async Task StrideSceneLifecycleActuator_DetachEntity_UsesStrideSceneDetach()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var actuator = new StrideSceneLifecycleActuator();

        await actuator.AttachEntityToSceneAsync(entity, scene);
        await actuator.DetachEntityFromSceneAsync(entity);

        Assert.Null(entity.Scene);
        Assert.DoesNotContain(entity, scene.Entities);
    }

    [Fact]
    public async Task StrideProcessorLifecycleActuator_AddRemoveProcessor_UsesEntityManagerProcessorCollection()
    {
        var manager = new SceneInstance(new ServiceRegistry());
        var processor = new Stride.Engine.Processors.TransformProcessor();
        var actuator = new StrideProcessorLifecycleActuator();

        await actuator.AddProcessorToSystemAsync(processor, manager);
        Assert.Same(processor, manager.GetProcessor<Stride.Engine.Processors.TransformProcessor>());

        await actuator.RemoveProcessorFromSystemAsync(processor, manager);
        Assert.Null(manager.GetProcessor<Stride.Engine.Processors.TransformProcessor>());
    }

    [Fact]
    public async Task StrideProcessorLifecycleActuator_AddEntity_UsesEngineSeam()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        var actuator = new StrideProcessorLifecycleActuator();
        manager.Processors.Add(processor);

        await actuator.AddEntityToProcessorAsync(processor, entity);

        Assert.Equal(1, processor.AddedCount);
    }

    [Fact]
    public async Task StrideProcessorLifecycleActuator_RemoveEntity_UsesEngineSeam()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        var actuator = new StrideProcessorLifecycleActuator();
        manager.Processors.Add(processor);
        await actuator.AddEntityToProcessorAsync(processor, entity);

        await actuator.RemoveEntityFromProcessorAsync(processor, entity);

        Assert.Equal(1, processor.RemovedCount);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_AddEntity_ThroughProductionAdapter_ReturnsCompletedEvent()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);
        var actuator = new StrideProcessorLifecycleActuator();

        var completed = await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(entity, completed.Entity);
        Assert.Equal(1, processor.AddedCount);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_RemoveEntity_ThroughProductionAdapter_ReturnsCompletedEvent()
    {
        var manager = CreateManagerWithEntity(out var entity);
        var processor = new RecordingProcessor();
        manager.Processors.Add(processor);
        var actuator = new StrideProcessorLifecycleActuator();
        await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), actuator);

        var completed = await ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(new ProcessorEntityRemoveRequested(processor, entity), actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(entity, completed.Entity);
        Assert.Equal(1, processor.RemovedCount);
    }

    [Fact]
    public async Task ProductionAdapters_WorkThroughTransitionHelpers()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var sceneEntity = new Entity("SceneEntity");
        var scene = new Scene();
        var transformActuator = new StrideTransformLifecycleActuator();
        var sceneActuator = new StrideSceneLifecycleActuator();

        var parentAttached = await TransformLifecycleTransition.AttachParentAsync(new TransformParentAttachRequested(child, parent), transformActuator);
        var entityAttached = await SceneLifecycleTransition.AttachEntityAsync(new EntitySceneAttachRequested(sceneEntity, scene), sceneActuator);

        Assert.Same(child, parentAttached.Child);
        Assert.Same(parent, parentAttached.Parent);
        Assert.Same(parent.Transform, child.Transform.Parent);

        Assert.Same(sceneEntity, entityAttached.Entity);
        Assert.Same(scene, entityAttached.Scene);
        Assert.Same(scene, sceneEntity.Scene);
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

        protected override void OnEntityComponentAdding(Entity entity, TestComponent component, TestComponent data) => AddedCount++;
        protected override void OnEntityComponentRemoved(Entity entity, TestComponent component, TestComponent data) => RemovedCount++;
    }
}
