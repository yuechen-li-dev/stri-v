using Xunit;

using Stride.Engine;

using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Runtime;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class TransformLifecycleRuntimeTests
{
    [Fact]
    public void DominatusRuntime_AttachTransformParent_ActsThroughProductionAdapter()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        var harness = new DominatusRuntimeTestHarness()
            .Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Attach",
            _ => TransformLifecycleDominatusNodes.AttachTransformParent(child, parent));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }
}
