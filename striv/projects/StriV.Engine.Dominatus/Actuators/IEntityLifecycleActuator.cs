using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface IEntityLifecycleActuator
{
    ValueTask AttachEntityAsync(Entity entity, CancellationToken cancellationToken = default);
    ValueTask DetachEntityAsync(Entity entity, CancellationToken cancellationToken = default);
}
