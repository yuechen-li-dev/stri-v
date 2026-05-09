using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Adapters.Cloning;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class EntityCloneLifecycleTests
{
    [Fact]
    public async Task EntityCloneTransition_CloneEntity_InvokesActuatorAndReturnsCompletedEvent()
    {
        var source = new Entity("CloneSource");
        source.Transform.Position = new Stride.Core.Mathematics.Vector3(1f, 2f, 3f);
        source.Transform.Scale = new Stride.Core.Mathematics.Vector3(1.5f, 2f, 2.5f);
        source.Add(new ModelComponent());

        var actuator = new CountingCloneActuator();
        var request = new EntityCloneRequested(source);

        var completed = await EntityCloneTransition.CloneEntityAsync(request, actuator);

        Assert.Equal(1, actuator.CloneCalls);
        Assert.Same(source, completed.Source);
        Assert.NotNull(completed.ClonedEntity);
        Assert.NotSame(source, completed.ClonedEntity);
        Assert.Equal(source.Name, completed.ClonedEntity.Name);
        Assert.Equal(source.Transform.Position, completed.ClonedEntity.Transform.Position);
        Assert.Equal(source.Transform.Scale, completed.ClonedEntity.Transform.Scale);
        Assert.Equal(source.Components.Count, completed.ClonedEntity.Components.Count);
        Assert.Contains(completed.ClonedEntity.Components, component => component is ModelComponent);
        Assert.Null(completed.ClonedEntity.Scene);
        Assert.Null(completed.ClonedEntity.Transform.Parent);
    }

    [Fact]
    public async Task EntityCloneTransition_CloneEntity_PropagatesActuatorFailure()
    {
        var source = new Entity("CloneSource");
        var request = new EntityCloneRequested(source);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await EntityCloneTransition.CloneEntityAsync(request, new ThrowingCloneActuator()));

        Assert.Equal("clone-failed", ex.Message);
    }

    [Fact]
    public async Task EntityCloneTransition_RejectsNullActuator()
    {
        var source = new Entity("CloneSource");
        var request = new EntityCloneRequested(source);

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await EntityCloneTransition.CloneEntityAsync(request, actuator: null!));
    }

    [Fact]
    public async Task EntityCloneNode_Surface_ExposesCloneIntent()
    {
        var source = new Entity("CloneSource");
        var request = EntityCloneNode.RequestClone(source);

        Assert.Same(source, request.Source);

        var completed = await EntityCloneNode.ExecuteCloneAsync(request, new CountingCloneActuator());

        Assert.Same(source, completed.Source);
        Assert.NotNull(completed.ClonedEntity);
        Assert.NotSame(source, completed.ClonedEntity);
    }

    [Fact]
    public async Task StrideEntityCloneActuator_CloneEntity_UsesCurrentEntityClonerApi()
    {
        var source = new Entity("CloneSource");
        var actuator = new StrideEntityCloneActuator();

        await Assert.ThrowsAsync<ArgumentException>(async () => await actuator.CloneEntityAsync(source));
    }

    private sealed class CountingCloneActuator : IEntityCloneActuator
    {
        public int CloneCalls { get; private set; }

        public ValueTask<Entity> CloneEntityAsync(Entity source, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(source);
            CloneCalls++;

            var clone = new Entity(source.Name)
            {
                Transform =
                {
                    Position = source.Transform.Position,
                    Rotation = source.Transform.Rotation,
                    Scale = source.Transform.Scale,
                },
            };

            foreach (var component in source.Components)
            {
                if (component is ModelComponent)
                    clone.Add(new ModelComponent());
            }

            return ValueTask.FromResult(clone);
        }
    }

    private sealed class ThrowingCloneActuator : IEntityCloneActuator
    {
        public ValueTask<Entity> CloneEntityAsync(Entity source, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("clone-failed");
    }
}
