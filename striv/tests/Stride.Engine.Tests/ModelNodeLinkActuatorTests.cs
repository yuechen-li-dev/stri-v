using Stride.Engine.Processors;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class ModelNodeLinkActuatorTests
{
    [Fact]
    public void ModelNodeLinkActuator_AttachModelNodeLink_SetsTransformLink()
    {
        var actuator = (IModelNodeLinkActuator)new ModelNodeLinkProcessor();
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var model = new ModelComponent();
        parent.Components.Add(model);

        var link = new ModelNodeTransformLink(model, "Root");

        actuator.AttachModelNodeLink(child.Transform, link);

        Assert.Same(link, child.Transform.TransformLink);
    }

    [Fact]
    public void ModelNodeLinkActuator_ClearModelNodeLink_ClearsTransformLink()
    {
        var actuator = (IModelNodeLinkActuator)new ModelNodeLinkProcessor();
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var model = new ModelComponent();
        parent.Components.Add(model);

        actuator.AttachModelNodeLink(child.Transform, new ModelNodeTransformLink(model, "Root"));

        actuator.ClearModelNodeLink(child.Transform);

        Assert.Null(child.Transform.TransformLink);
    }
}
