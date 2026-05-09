using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface ISceneLifecycleActuator
{
    ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default);
    ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default);

    ValueTask AttachEntityToSceneAsync(Entity entity, Scene scene, CancellationToken cancellationToken = default);
    ValueTask DetachEntityFromSceneAsync(Entity entity, CancellationToken cancellationToken = default);
}
