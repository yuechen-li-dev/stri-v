using Stride.Engine;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Transitions;

namespace StriV.Engine.Dominatus.Nodes;

public static class EntityCloneNode
{
    public static EntityCloneRequested RequestClone(Entity source) => new(source);

    public static ValueTask<EntityCloneCompleted> ExecuteCloneAsync(
        EntityCloneRequested request,
        IEntityCloneActuator actuator,
        CancellationToken cancellationToken = default)
        => EntityCloneTransition.CloneEntityAsync(request, actuator, cancellationToken);
}
