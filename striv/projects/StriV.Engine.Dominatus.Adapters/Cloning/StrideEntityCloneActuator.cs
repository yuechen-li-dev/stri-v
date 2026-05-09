using Stride.Engine;
using Stride.Engine.Design;
using StriV.Engine.Dominatus.Actuators;

namespace StriV.Engine.Dominatus.Adapters.Cloning;

public sealed class StrideEntityCloneActuator : IEntityCloneActuator
{
    public ValueTask<Entity> CloneEntityAsync(Entity source, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(source);

        var clone = EntityCloner.Clone(source);
        if (clone is null)
            throw new InvalidOperationException("Stride EntityCloner returned null.");

        return ValueTask.FromResult(clone);
    }
}
