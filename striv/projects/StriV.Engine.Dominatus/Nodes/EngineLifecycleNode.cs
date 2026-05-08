using Dominatus.Core.Nodes;
using Dominatus.Core.Runtime;
using Dominatus.OptFlow;

namespace StriV.Engine.Dominatus.Nodes;

public static class EngineLifecycleNode
{
    public static IEnumerator<AiStep> Idle(AiCtx _)
    {
        while (true)
            yield return Ai.Wait(0.1f);
    }
}
