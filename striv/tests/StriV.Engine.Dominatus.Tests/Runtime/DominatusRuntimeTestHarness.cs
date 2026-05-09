using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;

namespace StriV.Engine.Dominatus.Tests.Runtime;

internal sealed class DominatusRuntimeTestHarness
{
    private readonly ActuatorHost actuatorHost = new();

    public DominatusRuntimeTestHarness Register<TCommand>(IActuationHandler<TCommand> handler)
        where TCommand : IActuationCommand
    {
        actuatorHost.Register(handler);
        return this;
    }

    public AiAgent CreateAgent(string rootStateId, AiNode node)
    {
        var graph = new HfsmGraph { Root = new StateId(rootStateId) };
        graph.Add(new HfsmStateDef { Id = rootStateId, Node = node });
        return new AiAgent(new HfsmInstance(graph));
    }

    public AiWorld CreateWorld(params AiAgent[] agents)
    {
        var world = new AiWorld(actuatorHost);

        foreach (var agent in agents)
        {
            world.Add(agent);
            agent.Brain.Initialize(world, agent);
        }

        return world;
    }

    public static void Tick(AiWorld world, float dt = 0.016f, int count = 1)
    {
        for (var i = 0; i < count; i++)
        {
            world.Tick(dt);
        }
    }
}
