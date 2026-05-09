using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Transitions;
using Xunit;

using StriV.Engine.Dominatus.Tests.Adapters;

namespace StriV.Engine.Dominatus.Tests;

public sealed class TransformLifecycleBridgeTests
{
    [Fact]
    public async Task TransformLifecycleActuator_AttachParent_UsesExistingStrideParenting()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleTestAdapter();

        await actuator.AttachParentAsync(child, parent);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task TransformLifecycleActuator_DetachParent_UsesExistingStrideDetach()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleTestAdapter();

        await actuator.AttachParentAsync(child, parent);
        await actuator.DetachParentAsync(child);

        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public void TransformLifecycleEvents_CarryExpectedEntities()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        var attachRequested = new TransformParentAttachRequested(child, parent);
        var attached = new TransformParentAttached(child, parent);
        var detachRequested = new TransformParentDetachRequested(child);
        var detached = new TransformParentDetached(child);

        Assert.Same(child, attachRequested.Child);
        Assert.Same(parent, attachRequested.Parent);
        Assert.Same(child, attached.Child);
        Assert.Same(parent, attached.Parent);
        Assert.Same(child, detachRequested.Child);
        Assert.Same(child, detached.Child);
    }

    [Fact]
    public void EntityAttachmentNode_Surface_ExposesTransformLifecycleIntent()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        var attach = EntityAttachmentNode.RequestTransformAttach(child, parent);
        var detach = EntityAttachmentNode.RequestTransformDetach(child);

        Assert.Same(child, attach.Child);
        Assert.Same(parent, attach.Parent);
        Assert.Same(child, detach.Child);
    }

    [Fact]
    public async Task TransformLifecycleTransition_AttachParent_InvokesActuatorAndReturnsAttachedEvent()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleTestAdapter();
        var request = new TransformParentAttachRequested(child, parent);

        var completed = await TransformLifecycleTransition.AttachParentAsync(request, actuator);

        Assert.Same(child, completed.Child);
        Assert.Same(parent, completed.Parent);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
        Assert.Equal(1, actuator.AttachCalls);
    }

    [Fact]
    public async Task TransformLifecycleTransition_DetachParent_InvokesActuatorAndReturnsDetachedEvent()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new StrideTransformLifecycleTestAdapter();

        await actuator.AttachParentAsync(child, parent);
        var request = new TransformParentDetachRequested(child);

        var completed = await TransformLifecycleTransition.DetachParentAsync(request, actuator);

        Assert.Same(child, completed.Child);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
        Assert.Equal(1, actuator.DetachCalls);
    }

    [Fact]
    public async Task TransformLifecycleTransition_AttachParent_PropagatesActuatorFailure()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var request = new TransformParentAttachRequested(child, parent);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await TransformLifecycleTransition.AttachParentAsync(request, new ThrowingTransformLifecycleActuator()));

        Assert.Equal("attach-failed", ex.Message);
    }

    [Fact]
    public async Task TransformLifecycleTransition_RejectsNullActuator()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var attachRequest = new TransformParentAttachRequested(child, parent);
        var detachRequest = new TransformParentDetachRequested(child);

        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await TransformLifecycleTransition.AttachParentAsync(attachRequest, actuator: null!));
        await Assert.ThrowsAsync<ArgumentNullException>(
            async () => await TransformLifecycleTransition.DetachParentAsync(detachRequest, actuator: null!));
    }

    private sealed class ThrowingTransformLifecycleActuator : ITransformLifecycleActuator
    {
        public ValueTask AttachParentAsync(Entity child, Entity parent, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("attach-failed");

        public ValueTask DetachParentAsync(Entity child, CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("detach-failed");
    }
}
