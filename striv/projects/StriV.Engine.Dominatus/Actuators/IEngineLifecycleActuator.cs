namespace StriV.Engine.Dominatus.Actuators;

public interface IEngineLifecycleActuator
{
    ValueTask StartAsync(CancellationToken cancellationToken = default);
    ValueTask StopAsync(CancellationToken cancellationToken = default);
}
