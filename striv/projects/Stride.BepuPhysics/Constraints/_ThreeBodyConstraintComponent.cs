// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constraint base for descriptions that bind exactly three body components.
/// </summary>
/// <remarks>
/// The body references exposed here are authoritative authoring state used by
/// <see cref="ConstraintProcessor"/>/<see cref="ConstraintComponent{T}"/> to build runtime
/// Bepu solver entries only after all referenced bodies have valid runtime handles.
/// Keep these slots as plain references; runtime handles remain derived state.
/// </remarks>
public abstract class ThreeBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IThreeBodyConstraintDescription<T>
{
    /// <summary>
    /// First body slot expected by the mapped Bepu three-body description.
    /// </summary>
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    /// <summary>
    /// Second body slot expected by the mapped Bepu three-body description.
    /// </summary>
    public BodyComponent? B
    {
        get => this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Third body slot expected by the mapped Bepu three-body description.
    /// </summary>
    public BodyComponent? C
    {
        get => this[2];
        set => this[2] = value;
    }

    public ThreeBodyConstraintComponent() : base(3) { }
}
