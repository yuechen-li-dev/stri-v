using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;

using Stride.Engine;

using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Runtime;

namespace StriV.Engine.Dominatus.Runtime;

public sealed class StriVEngineLifecycleRunner
{
    private const int MaxTicks = 1;
    private const float TickDeltaTime = 0.016f;

    public ValueTask AttachSceneTransformAndProcessorAsync(
        Scene scene,
        Entity parent,
        Entity child,
        EntityManager entityManager,
        EntityProcessor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(scene);
        ArgumentNullException.ThrowIfNull(parent);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(processor);

        cancellationToken.ThrowIfCancellationRequested();

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()));
        actuatorHost.Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));
        actuatorHost.Register(new ProcessorSystemAddActuationHandler(new StrideProcessorLifecycleActuator()));
        actuatorHost.Register(new ProcessorEntityAddActuationHandler(new StrideProcessorLifecycleActuator()));

        return RunSingleNodeAsync(
            actuatorHost,
            _ => EngineLifecycleDominatusNodes.AttachSceneTransformAndProcessor(scene, parent, child, entityManager, processor),
            cancellationToken);
    }

    public ValueTask CleanupProcessorLifecycleAsync(
        EntityManager entityManager,
        Entity child,
        EntityProcessor processor,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entityManager);
        ArgumentNullException.ThrowIfNull(child);
        ArgumentNullException.ThrowIfNull(processor);

        cancellationToken.ThrowIfCancellationRequested();

        var actuatorHost = new ActuatorHost();
        var processorActuator = new StrideProcessorLifecycleActuator();
        actuatorHost.Register(new ProcessorEntityRemoveActuationHandler(processorActuator));
        actuatorHost.Register(new ProcessorSystemRemoveActuationHandler(processorActuator));

        return RunSingleNodeAsync(
            actuatorHost,
            _ => ProcessorLifecycleDominatusNodes.RemoveProcessorAndEntity(processor, entityManager, child),
            cancellationToken);
    }

    private static ValueTask RunSingleNodeAsync(
        ActuatorHost actuatorHost,
        AiNode node,
        CancellationToken cancellationToken)
    {
        var graph = new HfsmGraph { Root = new StateId("Root") };
        graph.Add(new HfsmStateDef
        {
            Id = "Root",
            Node = node,
        });

        var agent = new AiAgent(new HfsmInstance(graph));
        var world = new AiWorld(actuatorHost);
        world.Add(agent);
        agent.Brain.Initialize(world, agent);

        for (var i = 0; i < MaxTicks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            world.Tick(TickDeltaTime);
        }

        return ValueTask.CompletedTask;
    }
}
