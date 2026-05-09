using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Events;

namespace StriV.Engine.Dominatus.Transitions;

public static class EntityCloneTransition
{
    public static async ValueTask<EntityCloneCompleted> CloneEntityAsync(
        EntityCloneRequested request,
        IEntityCloneActuator actuator,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request.Source);
        ArgumentNullException.ThrowIfNull(actuator);

        var clone = await actuator.CloneEntityAsync(request.Source, cancellationToken);
        if (clone is null)
            throw new InvalidOperationException("Entity clone actuator returned null.");

        return new EntityCloneCompleted(request.Source, clone);
    }
}
