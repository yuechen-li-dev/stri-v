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
