using Stride.Core;
using Stride.Engine;

using StriV.Engine.Dominatus.Runtime;

using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class StriVEngineLifecycleRunnerTests
{
    [Fact]
    public async Task StriVEngineLifecycleRunner_AttachSceneTransformAndProcessor_RunsThroughDominatusRuntime()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        var runner = new StriVEngineLifecycleRunner();

        await runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor);

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
    public async Task StriVEngineLifecycleRunner_AttachSceneTransformAndProcessor_RejectsNullArguments()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var processor = new RecordingProcessor();

        var runner = new StriVEngineLifecycleRunner();

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.AttachSceneTransformAndProcessorAsync(null!, parent, child, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.AttachSceneTransformAndProcessorAsync(scene, null!, child, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.AttachSceneTransformAndProcessorAsync(scene, parent, null!, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, null!, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, null!).AsTask());
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_AttachSceneTransformAndProcessor_CanceledBeforeStart_ThrowsOperationCanceledException()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        var runner = new StriVEngineLifecycleRunner();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor, cancellationTokenSource.Token).AsTask());

        Assert.Null(parent.Scene);
        Assert.Null(child.Scene);
        Assert.Null(child.Transform.Parent);
        Assert.Empty(parent.Transform.Children);
        Assert.Null(processor.EntityManager);
        Assert.Empty(processor.AddedEntities);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_CleanupProcessorLifecycle_RunsThroughDominatusRuntime()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();

        var runner = new StriVEngineLifecycleRunner();

        await runner.AttachSceneTransformAndProcessorAsync(scene, parent, child, entityManager, processor);

        Assert.Same(entityManager, processor.EntityManager);
        Assert.Equal(1, processor.AddedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));

        await runner.CleanupProcessorLifecycleAsync(entityManager, child, processor);

        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(child, Assert.Single(processor.RemovedEntities));
        Assert.Null(processor.EntityManager);
        Assert.DoesNotContain(processor, entityManager.Processors);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_CleanupProcessorLifecycle_RejectsNullArguments()
    {
        var entityManager = new SceneInstance(new ServiceRegistry());
        var child = new Entity("Child");
        var processor = new RecordingProcessor();

        var runner = new StriVEngineLifecycleRunner();

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.CleanupProcessorLifecycleAsync(null!, child, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.CleanupProcessorLifecycleAsync(entityManager, null!, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.CleanupProcessorLifecycleAsync(entityManager, child, null!).AsTask());
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_CleanupProcessorLifecycle_CanceledBeforeStart_ThrowsOperationCanceledException()
    {
        var entityManager = new SceneInstance(new ServiceRegistry());
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();
        entityManager.Processors.Add(processor);

        var runner = new StriVEngineLifecycleRunner();
        using var cancellationTokenSource = new CancellationTokenSource();
        cancellationTokenSource.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(() => runner.CleanupProcessorLifecycleAsync(entityManager, child, processor, cancellationTokenSource.Token).AsTask());

        Assert.Same(entityManager, processor.EntityManager);
        Assert.Contains(processor, entityManager.Processors);
        Assert.Empty(processor.RemovedEntities);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_DetachTransformParent_RunsThroughDominatusRuntime()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Transform.Parent = parent.Transform;
        var runner = new StriVEngineLifecycleRunner();

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);

        await runner.DetachTransformParentAsync(child);

        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_DetachEntityFromScene_RunsThroughDominatusRuntime()
    {
        var scene = new Scene();
        var entity = new Entity("Entity");
        entity.Scene = scene;
        var runner = new StriVEngineLifecycleRunner();

        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);

        await runner.DetachEntityFromSceneAsync(entity);

        Assert.Null(entity.Scene);
        Assert.DoesNotContain(entity, scene.Entities);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_DetachCleanupMethods_RejectNullArguments()
    {
        var runner = new StriVEngineLifecycleRunner();

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.DetachTransformParentAsync(null!).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.DetachEntityFromSceneAsync(null!).AsTask());
    }

    [Fact]
    public void StriVEngineLifecycleRunner_InvalidMaxTicks_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StriVEngineLifecycleRunner(new StriVEngineLifecycleRunnerOptions { MaxTicks = 0 }));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public void StriVEngineLifecycleRunner_InvalidFixedDelta_Throws()
    {
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => new StriVEngineLifecycleRunner(new StriVEngineLifecycleRunnerOptions { FixedDeltaSeconds = 0f }));
        Assert.Equal("options", exception.ParamName);
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_RunSceneTransformProcessorFullCycle_RunsThroughDominatusRuntime()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        child.Components.Add(new TestComponent());
        var processor = new RecordingProcessor();
        var runner = new StriVEngineLifecycleRunner();

        await runner.RunSceneTransformProcessorFullCycleAsync(scene, parent, child, entityManager, processor);

        Assert.Null(parent.Scene);
        Assert.Null(child.Scene);
        Assert.DoesNotContain(parent, scene.Entities);
        Assert.DoesNotContain(child, scene.Entities);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
        Assert.Null(processor.EntityManager);
        Assert.DoesNotContain(processor, entityManager.Processors);
        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(child, Assert.Single(processor.AddedEntities));
        Assert.Same(child, Assert.Single(processor.RemovedEntities));
    }

    [Fact]
    public async Task StriVEngineLifecycleRunner_RunSceneTransformProcessorFullCycle_RejectsNullArguments()
    {
        var scene = new Scene();
        var entityManager = new SceneInstance(new ServiceRegistry());
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var processor = new RecordingProcessor();
        var runner = new StriVEngineLifecycleRunner();

        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunSceneTransformProcessorFullCycleAsync(null!, parent, child, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunSceneTransformProcessorFullCycleAsync(scene, null!, child, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunSceneTransformProcessorFullCycleAsync(scene, parent, null!, entityManager, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunSceneTransformProcessorFullCycleAsync(scene, parent, child, null!, processor).AsTask());
        await Assert.ThrowsAsync<ArgumentNullException>(() => runner.RunSceneTransformProcessorFullCycleAsync(scene, parent, child, entityManager, null!).AsTask());
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
