using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class TransformHierarchyActuatorTests
{
    [Fact]
    public void TransformHierarchyActuator_AttachParent_UpdatesParentAndChildren()
    {
        var actuator = new TransformComponent.TransformHierarchyActuator();
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        actuator.AttachParent(child.Transform, parent.Transform);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public void TransformHierarchyActuator_DetachParent_ClearsParentAndChildCollection()
    {
        var actuator = new TransformComponent.TransformHierarchyActuator();
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        actuator.AttachParent(child.Transform, parent.Transform);
        actuator.DetachParent(child.Transform);

        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public void TransformHierarchyActuator_Reparent_UpdatesOldAndNewParentCollections()
    {
        var actuator = new TransformComponent.TransformHierarchyActuator();
        var oldParent = new Entity("OldParent");
        var newParent = new Entity("NewParent");
        var child = new Entity("Child");

        actuator.AttachParent(child.Transform, oldParent.Transform);
        actuator.AttachParent(child.Transform, newParent.Transform);

        Assert.DoesNotContain(child.Transform, oldParent.Transform.Children);
        Assert.Contains(child.Transform, newParent.Transform.Children);
        Assert.Same(newParent.Transform, child.Transform.Parent);
    }

    [Fact]
    public void TransformComponent_ParentSetter_StillMaintainsHierarchy()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        child.Transform.Parent = parent.Transform;
        Assert.Contains(child.Transform, parent.Transform.Children);

        child.Transform.Parent = null;
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }
}
