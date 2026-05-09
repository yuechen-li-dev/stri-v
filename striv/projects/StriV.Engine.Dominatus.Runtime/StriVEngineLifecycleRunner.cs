using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;

using Stride.Engine;

using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Nodes;

namespace StriV.Engine.Dominatus.Runtime;

public sealed record StriVEngineLifecycleRunnerOptions
{
    public int MaxTicks { get; init; } = 1;
    public float FixedDeltaSeconds { get; init; } = 1f / 60f;
}

public sealed class StriVEngineLifecycleRunner
{
    private readonly int maxTicks;
    private readonly float fixedDeltaSeconds;

    public StriVEngineLifecycleRunner(StriVEngineLifecycleRunnerOptions? options = null)
    {
        var resolvedOptions = options ?? new StriVEngineLifecycleRunnerOptions();

        if (resolvedOptions.MaxTicks <= 0)
            throw new ArgumentOutOfRangeException(nameof(options), "MaxTicks must be greater than zero.");

        if (resolvedOptions.FixedDeltaSeconds <= 0f)
            throw new ArgumentOutOfRangeException(nameof(options), "FixedDeltaSeconds must be greater than zero.");

        maxTicks = resolvedOptions.MaxTicks;
        fixedDeltaSeconds = resolvedOptions.FixedDeltaSeconds;
    }

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

    public ValueTask DetachTransformParentAsync(
        Entity child,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(child);
        cancellationToken.ThrowIfCancellationRequested();

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new TransformParentDetachActuationHandler(new StrideTransformLifecycleActuator()));

        return RunSingleNodeAsync(
            actuatorHost,
            ctx => TransformLifecycleDominatusNodes.DetachTransformParent(ctx, child),
            cancellationToken);
    }

    public ValueTask DetachEntityFromSceneAsync(
        Entity entity,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entity);
        cancellationToken.ThrowIfCancellationRequested();

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new EntitySceneDetachActuationHandler(new StrideSceneLifecycleActuator()));

        return RunSingleNodeAsync(
            actuatorHost,
            ctx => SceneLifecycleDominatusNodes.DetachEntityFromScene(ctx, entity),
            cancellationToken);
    }

    public ValueTask RunSceneTransformProcessorFullCycleAsync(
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
        var sceneActuator = new StrideSceneLifecycleActuator();
        var transformActuator = new StrideTransformLifecycleActuator();
        var processorActuator = new StrideProcessorLifecycleActuator();
        actuatorHost.Register(new EntitySceneAttachActuationHandler(sceneActuator));
        actuatorHost.Register(new EntitySceneDetachActuationHandler(sceneActuator));
        actuatorHost.Register(new TransformParentAttachActuationHandler(transformActuator));
        actuatorHost.Register(new TransformParentDetachActuationHandler(transformActuator));
        actuatorHost.Register(new ProcessorSystemAddActuationHandler(processorActuator));
        actuatorHost.Register(new ProcessorEntityAddActuationHandler(processorActuator));
        actuatorHost.Register(new ProcessorEntityRemoveActuationHandler(processorActuator));
        actuatorHost.Register(new ProcessorSystemRemoveActuationHandler(processorActuator));

        return RunSingleNodeAsync(
            actuatorHost,
            _ => EngineLifecycleDominatusNodes.RunSceneTransformProcessorFullCycle(scene, parent, child, entityManager, processor),
            cancellationToken);
    }

    private ValueTask RunSingleNodeAsync(
        ActuatorHost actuatorHost,
        AiNode node,
        CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var graph = new HfsmGraph { Root = new StateId("Root") };
        graph.Add(new HfsmStateDef
        {
            Id = "Root",
            Node = node,
        });

        var brain = new HfsmInstance(graph);
        var agent = new AiAgent(brain);
        var world = new AiWorld(actuatorHost);
        world.Add(agent);
        brain.Initialize(world, agent);

        for (var i = 0; i < maxTicks; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            world.Tick(fixedDeltaSeconds);

            if (brain.GetActivePath().Count == 0)
                break;
        }

        return ValueTask.CompletedTask;
    }
}
