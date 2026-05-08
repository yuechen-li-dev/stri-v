using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;
using Dominatus.OptFlow;

using Stride.Engine;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Nodes;

public static class EntityAttachmentNode
{
    public static TransformParentAttachRequested RequestTransformAttach(Entity child, Entity parent) => new(child, parent);

    public static TransformParentDetachRequested RequestTransformDetach(Entity child) => new(child);

    public static IEnumerator<AiStep> Idle(AiCtx _)
    {
        while (true)
            yield return Ai.Wait(0.1f);
    }
}
