using Dominatus.Core.Nodes;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Nodes;

public static class SceneLifecycleDominatusNodes
{
    public static IEnumerator<AiStep> AttachEntityToScene(Entity entity, Scene scene)
    {
        yield return Ai.Act(new EntitySceneAttachRequested(entity, scene));
    }
}
