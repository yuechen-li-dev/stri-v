using Xunit;

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

        var harness = new DominatusRuntimeTestHarness()
            .Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Attach",
            _ => SceneLifecycleDominatusNodes.AttachEntityToScene(entity, scene));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public void DominatusRuntime_SceneThenTransformAttach_ComposesThroughProductionAdapters()
    {
        var scene = new Scene();
        var parent = new Entity("Parent");
        var child = new Entity("Child");

        var harness = new DominatusRuntimeTestHarness()
            .Register(new EntitySceneAttachActuationHandler(new StrideSceneLifecycleActuator()))
            .Register(new TransformParentAttachActuationHandler(new StrideTransformLifecycleActuator()));

        var agent = harness.CreateAgent(
            "Compose",
            _ => ComposeSceneThenTransform(scene, parent, child));

        var world = harness.CreateWorld(agent);
        DominatusRuntimeTestHarness.Tick(world);

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
