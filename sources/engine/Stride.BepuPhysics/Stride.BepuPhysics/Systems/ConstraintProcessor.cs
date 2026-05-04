// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Constraints;
using Stride.BepuPhysics.Definitions;
using Stride.Engine;

namespace Stride.BepuPhysics.Systems;

/// <summary>
/// Runtime processor that activates/deactivates constraint components against the current <see cref="BepuConfiguration"/>.
/// </summary>
/// <remarks>
/// Component fields remain the authoritative serialized state; this processor only wires lifecycle events.
/// Any runtime Bepu handles are owned by individual constraint components and rebuilt from component references.
/// </remarks>
public class ConstraintProcessor : EntityProcessor<ConstraintComponentBase>
{
    // Service-lifetime dependency resolved once when the processor is attached to the game.
    // Null-forgiving is intentional: assignment happens in OnSystemAdd before any component callbacks run.
    private BepuConfiguration _bepuConfiguration = null!;

    public ConstraintProcessor()
    {
        Order = SystemsOrderHelper.ORDER_OF_CONSTRAINT_P;
    }

    protected override void OnSystemAdd()
    {
        // Centralized configuration ensures all constraints map to the same simulation selection rules.
        _bepuConfiguration = Services.GetOrCreate<BepuConfiguration>();
    }

    protected override void OnEntityComponentAdding(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        // Activation must be lightweight and idempotent: body handles may not be available yet.
        component.Activate(_bepuConfiguration);
    }

    protected override void OnEntityComponentRemoved(Entity entity, ConstraintComponentBase component, ConstraintComponentBase data)
    {
        // Always request component-side teardown so solver handles do not outlive scene membership.
        component.Deactivate();
    }
}
