using Xunit;
using Dominatus.Core;
using Dominatus.Core.Hfsm;
using Dominatus.Core.Runtime;
using Dominatus.Core.Nodes;

using Stride.Engine;
using StriV.Engine.Dominatus.Adapters;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;
using StriV.Engine.Dominatus.Runtime;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class SceneLifecycleRuntimeTests
{
    [Fact]
    public void DominatusRuntime_AttachEntityToScene_ActsThroughProductionAdapter()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();

        var graph = new HfsmGraph { Root = new StateId("Attach") };
        graph.Add(new HfsmStateDef
        {
            Id = "Attach",
            Node = _ => SceneLifecycleDominatusNodes.AttachEntityToScene(entity, scene),
        });

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()));

        var world = new AiWorld(actuatorHost);
        var agent = new AiAgent(new HfsmInstance(graph));
        world.Add(agent);
        agent.Brain.Initialize(world, agent);

        world.Tick(0.016f);

        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public void DominatusRuntime_SceneThenTransformAttach_ComposesThroughProductionAdapters()
    {
        var scene = new Scene();
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        var graph = new HfsmGraph { Root = new StateId("Compose") };
        graph.Add(new HfsmStateDef
        {
            Id = "Compose",
            Node = _ => ComposeSceneThenTransform(scene, parent, child),
        });

        var actuatorHost = new ActuatorHost();
        actuatorHost.Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()));
        actuatorHost.Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));

        var world = new AiWorld(actuatorHost);
        var agent = new AiAgent(new HfsmInstance(graph));
        world.Add(agent);
        agent.Brain.Initialize(world, agent);

        world.Tick(0.016f);

        Assert.Same(scene, parent.Scene);
        Assert.Same(scene, child.Scene);
        Assert.Contains(parent, scene.Entities);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    private static IEnumerator<AiStep> ComposeSceneThenTransform(Scene scene, Entity parent, Entity child)
    {
        yield return global::Dominatus.OptFlow.Ai.Act(new EntitySceneAttachRequested(parent, scene));
        yield return global::Dominatus.OptFlow.Ai.Act(new EntitySceneAttachRequested(child, scene));
        yield return global::Dominatus.OptFlow.Ai.Act(new TransformParentAttachRequested(child, parent));
    }
}
