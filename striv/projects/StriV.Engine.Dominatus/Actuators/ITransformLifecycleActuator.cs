using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface ITransformLifecycleActuator
{
    ValueTask AttachParentAsync(Entity child, Entity parent, CancellationToken cancellationToken = default);
    ValueTask DetachParentAsync(Entity child, CancellationToken cancellationToken = default);
}
