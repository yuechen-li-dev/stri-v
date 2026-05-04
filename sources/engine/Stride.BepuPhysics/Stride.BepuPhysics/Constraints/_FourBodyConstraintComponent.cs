// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constraint base for descriptions that bind exactly four body components.
/// </summary>
/// <remarks>
/// These slots capture serialized/component references only. Runtime Bepu constraint
/// handles are created and destroyed by <see cref="ConstraintComponent{T}"/> during
/// attach/detach, and may legitimately be absent while references are unresolved.
/// </remarks>
public abstract class FourBodyConstraintComponent<T> : ConstraintComponent<T> where T : unmanaged, IConstraintDescription<T>, IFourBodyConstraintDescription<T>
{
    /// <summary>
    /// First body slot expected by the mapped Bepu four-body description.
    /// </summary>
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    /// <summary>
    /// Second body slot expected by the mapped Bepu four-body description.
    /// </summary>
    public BodyComponent? B
    {
        get => this[1];
        set => this[1] = value;
    }

    /// <summary>
    /// Third body slot expected by the mapped Bepu four-body description.
    /// </summary>
    public BodyComponent? C
    {
        get => this[2];
        set => this[2] = value;
    }

    /// <summary>
    /// Fourth body slot expected by the mapped Bepu four-body description.
    /// </summary>
    public BodyComponent? D
    {
        get => this[3];
        set => this[3] = value;
    }

    public FourBodyConstraintComponent() : base(4){ }
}
