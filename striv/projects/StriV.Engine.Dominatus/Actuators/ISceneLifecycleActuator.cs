using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface ISceneLifecycleActuator
{
    ValueTask AttachSceneAsync(Scene scene, CancellationToken cancellationToken = default);
    ValueTask DetachSceneAsync(Scene scene, CancellationToken cancellationToken = default);
}
