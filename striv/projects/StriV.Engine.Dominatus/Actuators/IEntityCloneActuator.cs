using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface IEntityCloneActuator
{
    ValueTask<Entity> CloneEntityAsync(Entity source, CancellationToken cancellationToken = default);
}
