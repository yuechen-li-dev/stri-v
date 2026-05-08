using Dominatus.Core.Blackboard;
using Dominatus.Core.Runtime;
using Stride.Engine;

namespace StriV.Engine.Dominatus.Blackboard;

public static class EngineBlackboardKeys
{
    public static readonly BbKey<string> EngineLifecyclePhase = new("StriV.Engine.Lifecycle.Phase");
    public static readonly BbKey<string> SceneLifecyclePhase = new("StriV.Scene.Lifecycle.Phase");
    public static readonly BbKey<string> EntityAttachmentState = new("StriV.Entity.Attachment.State");
    public static readonly BbKey<string> ProcessorLifecycleState = new("StriV.Processor.Lifecycle.State");

    public static readonly BbKey<Scene> ActiveScene = new("StriV.Scene.Active");
    public static readonly BbKey<SceneInstance> ActiveSceneInstance = new("StriV.Scene.ActiveInstance");
    public static readonly BbKey<EntityManager> EntityManager = new("StriV.Entity.Manager");
    public static readonly BbKey<Entity> CurrentEntity = new("StriV.Entity.Current");
    public static readonly BbKey<EntityProcessor> CurrentProcessor = new("StriV.Processor.Current");

    public static readonly BbKey<ActuationId> PendingLifecycleActuationId = new("StriV.Engine.Lifecycle.PendingActuationId");
}
