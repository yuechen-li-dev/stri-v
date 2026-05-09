using Xunit;
using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Runtime;

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

        var graph = new HfsmGraph { Root = new StateId("Attach") };
        graph.Add(new HfsmStateDef
        {
            Id = "Attach",
            Node = _ => TransformLifecycleDominatusNodes.AttachTransformParent(child, parent),
        });

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));

        var world = new AiWorld(actuatorHost);
        var agent = new AiAgent(new HfsmInstance(graph));
        world.Add(agent);
        agent.Brain.Initialize(world, agent);

        world.Tick(0.016f);

        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }
}
