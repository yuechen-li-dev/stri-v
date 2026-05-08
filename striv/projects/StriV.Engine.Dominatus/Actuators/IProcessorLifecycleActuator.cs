using Stride.Engine;

namespace StriV.Engine.Dominatus.Actuators;

public interface IProcessorLifecycleActuator
{
    ValueTask AddProcessorAsync(EntityProcessor processor, CancellationToken cancellationToken = default);
    ValueTask RemoveProcessorAsync(EntityProcessor processor, CancellationToken cancellationToken = default);
}
