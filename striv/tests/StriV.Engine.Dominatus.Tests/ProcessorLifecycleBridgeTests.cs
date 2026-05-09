using Stride.Core;
using Stride.Engine;
using Stride.Engine.Processors;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class ProcessorLifecycleBridgeTests
{
    [Fact]
    public async Task ProcessorLifecycleTransition_AddProcessorToSystem_InvokesActuatorAndReturnsCompletedEvent()
    {
        var processor = new TransformProcessor();
        var manager = new SceneInstance(new ServiceRegistry());
        var actuator = new RecordingProcessorLifecycleActuator();
        var request = new ProcessorSystemAddRequested(processor, manager);

        var completed = await ProcessorLifecycleTransition.AddProcessorToSystemAsync(request, actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(manager, completed.EntityManager);
        Assert.Equal(1, actuator.SystemAddCalls);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_RemoveProcessorFromSystem_InvokesActuatorAndReturnsCompletedEvent()
    {
        var processor = new TransformProcessor();
        var manager = new SceneInstance(new ServiceRegistry());
        var actuator = new RecordingProcessorLifecycleActuator();
        var request = new ProcessorSystemRemoveRequested(processor, manager);

        var completed = await ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(request, actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(manager, completed.EntityManager);
        Assert.Equal(1, actuator.SystemRemoveCalls);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_AddEntityToProcessor_InvokesActuatorAndReturnsCompletedEvent()
    {
        var processor = new TransformProcessor();
        var entity = new Entity("Entity");
        var actuator = new RecordingProcessorLifecycleActuator();

        var completed = await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(entity, completed.Entity);
        Assert.Equal(1, actuator.EntityAddCalls);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_RemoveEntityFromProcessor_InvokesActuatorAndReturnsCompletedEvent()
    {
        var processor = new TransformProcessor();
        var entity = new Entity("Entity");
        var actuator = new RecordingProcessorLifecycleActuator();

        var completed = await ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(new ProcessorEntityRemoveRequested(processor, entity), actuator);

        Assert.Same(processor, completed.Processor);
        Assert.Same(entity, completed.Entity);
        Assert.Equal(1, actuator.EntityRemoveCalls);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_PropagatesActuatorFailure()
    {
        var request = new ProcessorSystemAddRequested(new TransformProcessor(), new SceneInstance(new ServiceRegistry()));

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await ProcessorLifecycleTransition.AddProcessorToSystemAsync(request, new ThrowingProcessorLifecycleActuator()));

        Assert.Equal("system-add-failed", ex.Message);
    }

    [Fact]
    public async Task ProcessorLifecycleTransition_RejectsNullActuator()
    {
        var processor = new TransformProcessor();
        var manager = new SceneInstance(new ServiceRegistry());
        var entity = new Entity("Entity");

        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessorLifecycleTransition.AddProcessorToSystemAsync(new ProcessorSystemAddRequested(processor, manager), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessorLifecycleTransition.RemoveProcessorFromSystemAsync(new ProcessorSystemRemoveRequested(processor, manager), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessorLifecycleTransition.AddEntityToProcessorAsync(new ProcessorEntityAddRequested(processor, entity), null!));
        await Assert.ThrowsAsync<ArgumentNullException>(async () => await ProcessorLifecycleTransition.RemoveEntityFromProcessorAsync(new ProcessorEntityRemoveRequested(processor, entity), null!));
    }

    [Fact]
    public async Task ProcessorLifecycleNode_Surface_ExposesLifecycleIntent()
    {
        var processor = new TransformProcessor();
        var manager = new SceneInstance(new ServiceRegistry());
        var entity = new Entity("Entity");
        var actuator = new RecordingProcessorLifecycleActuator();

        var addReq = ProcessorLifecycleNode.RequestSystemAdd(processor, manager);
        var removeReq = ProcessorLifecycleNode.RequestSystemRemove(processor, manager);
        var addEntityReq = ProcessorLifecycleNode.RequestEntityAdd(processor, entity);
        var removeEntityReq = ProcessorLifecycleNode.RequestEntityRemove(processor, entity);

        await ProcessorLifecycleNode.ExecuteSystemAddAsync(addReq, actuator);
        await ProcessorLifecycleNode.ExecuteSystemRemoveAsync(removeReq, actuator);
        await ProcessorLifecycleNode.ExecuteEntityAddAsync(addEntityReq, actuator);
        await ProcessorLifecycleNode.ExecuteEntityRemoveAsync(removeEntityReq, actuator);

        Assert.Equal(1, actuator.SystemAddCalls);
        Assert.Equal(1, actuator.SystemRemoveCalls);
        Assert.Equal(1, actuator.EntityAddCalls);
        Assert.Equal(1, actuator.EntityRemoveCalls);
    }

    private sealed class RecordingProcessorLifecycleActuator : IProcessorLifecycleActuator
    {
        public int SystemAddCalls { get; private set; }
        public int SystemRemoveCalls { get; private set; }
        public int EntityAddCalls { get; private set; }
        public int EntityRemoveCalls { get; private set; }

        public ValueTask AddProcessorToSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
        {
            SystemAddCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveProcessorFromSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
        {
            SystemRemoveCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask AddEntityToProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
        {
            EntityAddCalls++;
            return ValueTask.CompletedTask;
        }

        public ValueTask RemoveEntityFromProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
        {
            EntityRemoveCalls++;
            return ValueTask.CompletedTask;
        }
    }

    private sealed class ThrowingProcessorLifecycleActuator : IProcessorLifecycleActuator
    {
        public ValueTask AddProcessorToSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("system-add-failed");

        public ValueTask RemoveProcessorFromSystemAsync(EntityProcessor processor, EntityManager entityManager, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("system-remove-failed");

        public ValueTask AddEntityToProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("entity-add-failed");

        public ValueTask RemoveEntityFromProcessorAsync(EntityProcessor processor, Entity entity, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("entity-remove-failed");
    }
}
