// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Base component for constraints that bind a single body.
/// </summary>
public abstract class OneBodyConstraintComponent<T> : ConstraintComponent<T>, IOneBody where T : unmanaged, IConstraintDescription<T>, IOneBodyConstraintDescription<T>
{
    /// <summary>
    /// Constrained body.
    /// </summary>
    public BodyComponent? A
    {
        get => this[0];
        set => this[0] = value;
    }

    public OneBodyConstraintComponent() : base(1){ }
}
