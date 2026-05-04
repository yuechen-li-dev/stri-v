// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Base component for constraints that bind exactly two bodies.
/// </summary>
/// <remarks>
/// The body references are serialized component links. Runtime handle resolution happens later through <see cref="ConstraintProcessor"/>.
/// </remarks>
public abstract class TwoBodyConstraintComponent<T> : ConstraintComponent<T>, ITwoBody where T : unmanaged, IConstraintDescription<T>, ITwoBodyConstraintDescription<T>
{
    /// <summary>
    /// First constrained body.
    /// </summary>
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    /// <summary>
    /// Second constrained body.
    /// </summary>
    public BodyComponent? B
    {
        get => this[1];
        set => this[1] = value;
    }

    public TwoBodyConstraintComponent() : base(2) { }
}
