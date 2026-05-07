// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Diagnostics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

[DataContract(Inherited = true)]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
[AllowMultipleComponents]
/// <summary>
/// Base type for Stride-side constraint components that are materialized into Bepu solver constraints at runtime.
/// </summary>
/// <remarks>
/// This type stores serialized/editor-facing references and defers runtime binding to <see cref="ConstraintProcessor"/>.
/// Constraints are expected to survive arbitrary scene/entity load order; body handles may not exist when the component is deserialized.
/// </remarks>
public abstract class ConstraintComponentBase : EntityComponent
{
    protected static Logger Logger = GlobalLogger.GetLogger(nameof(ConstraintComponentBase));

    private bool _enabled = true;
    private readonly BodyComponent?[] _bodies;

    /// <summary>
    /// Enables or disables runtime attachment for this constraint description.
    /// </summary>
    /// <remarks>
    /// Changing this value does not mutate serialized shape; it only asks the runtime to detach or reattach in place.
    /// </remarks>
    public bool Enabled
    {
        get
        {
            return _enabled;
        }
        set
        {
            _enabled = value;
            TryReattachConstraint();
        }
    }

    /// <summary>
    /// Gets the body slots this constraint expects to bind to.
    /// </summary>
    public ReadOnlySpan<BodyComponent?> Bodies => _bodies;

    protected ConstraintComponentBase(int bodies) => _bodies = new BodyComponent?[bodies];

    protected BodyComponent? this[int i]
    {
        get => _bodies[i];
        set
        {
            _bodies[i] = value;
            BodiesChanged();
        }
    }

    /// <summary>
    /// Whether this constraint is in a valid state and actively constraining its targets.
    /// </summary>
    /// <remarks> May not be attached if it is not in a scene, when not <see cref="Enabled"/>, when any of its target is null, not in a scene or in a different simulation </remarks>
    public abstract bool Attached { get; }

    /// <summary>
    /// Returns the sum of all impulses this constraint applied on the last tick
    /// </summary>
    /// <remarks>
    /// Impulses increase depending on <see cref="BepuSimulation.FixedTimeStep"/>, as well as the amount of <see cref="BepuSimulation.SolverSubStep"/>.
    /// You may want to use <see cref="GetAccumulatedForceMagnitude"/> instead.
    /// </remarks>
    public abstract float GetAccumulatedImpulseMagnitude();

    /// <summary>
    /// Returns the sum of all forces this constraint applied on the last tick
    /// </summary>
    /// <remarks>
    /// This can be used to compare with a given motor constraints' MaximumForce property for example.
    /// </remarks>
    public abstract float GetAccumulatedForceMagnitude();

    /// <summary>
    /// Called whenever one of the body slots changes.
    /// </summary>
    /// <remarks>
    /// Implementations should keep this side effect free relative to serialized state and only coordinate runtime registration.
    /// </remarks>
    protected abstract void BodiesChanged();

    /// <summary>
    /// Called by <see cref="ConstraintProcessor"/> when the component becomes active in a scene with a Bepu configuration.
    /// </summary>
    internal abstract void Activate(BepuConfiguration bepuConfig);

    internal abstract void Deactivate();

    /// <summary>
    /// Attempts to detach/rebuild the runtime solver entry from the current serialized component state.
    /// </summary>
    internal abstract ConstraintState TryReattachConstraint();

    internal abstract void DetachConstraint();

    public enum ConstraintState
    {
        ConstraintNotInScene,
        ConstraintDisabled,
        BodyNotInScene,
        BodyNull,
        SimulationMismatch,
        FullyOperational,
    }
}
