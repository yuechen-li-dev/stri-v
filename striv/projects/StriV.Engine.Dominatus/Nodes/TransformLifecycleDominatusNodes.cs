using Dominatus.Core.Nodes;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Nodes;

public static class TransformLifecycleDominatusNodes
{
    public static IEnumerator<AiStep> AttachTransformParent(Entity child, Entity parent)
    {
        yield return Ai.Act(new TransformParentAttachRequested(child, parent));
    }
}
