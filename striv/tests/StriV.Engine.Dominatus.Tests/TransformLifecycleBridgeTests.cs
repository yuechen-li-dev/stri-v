using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class TransformLifecycleBridgeTests
{
    [Fact]
    public async Task TransformLifecycleActuator_AttachParent_UsesExistingStrideParenting()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new FakeTransformLifecycleActuator();

        await actuator.AttachParentAsync(child, parent);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task TransformLifecycleActuator_DetachParent_UsesExistingStrideDetach()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var actuator = new FakeTransformLifecycleActuator();

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

    private sealed class FakeTransformLifecycleActuator : ITransformLifecycleActuator
    {
        public ValueTask AttachParentAsync(Entity child, Entity parent, CancellationToken cancellationToken = default)
        {
            child.Transform.Parent = parent.Transform;
            return ValueTask.CompletedTask;
        }

        public ValueTask DetachParentAsync(Entity child, CancellationToken cancellationToken = default)
        {
            child.Transform.Parent = null;
            return ValueTask.CompletedTask;
        }
    }
}
