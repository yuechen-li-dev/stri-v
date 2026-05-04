// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using BepuPhysics.Constraints;
using Stride.BepuPhysics.Systems;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;
using Stride.Engine.Design;

namespace Stride.BepuPhysics.Constraints;

/// <summary>
/// Constrains two bodies so they rotate around one shared hinge axis while remaining connected at local offsets.
/// </summary>
/// <remarks>
/// Component properties mirror a Bepu <see cref="Hinge"/> description. <see cref="ConstraintProcessor"/> materializes this
/// description when both body handles are available in the same simulation.
/// </remarks>
[DataContract]
[DefaultEntityComponentProcessor(typeof(ConstraintProcessor), ExecutionMode = ExecutionMode.Runtime)]
[ComponentCategory("Physics - Bepu Constraint")]
public sealed class HingeConstraintComponent : TwoBodyConstraintComponent<Hinge>, ISpring, IWithTwoLocalOffset
{
    /// <summary>
    /// Initializes default spring settings for a practical out-of-box hinge response.
    /// </summary>
    public HingeConstraintComponent() => BepuConstraint = new()
    {
        SpringSettings = new SpringSettings(30, 5)
    };

    /// <inheritdoc/>
    public Vector3 LocalOffsetA
    {
        get
        {
            return BepuConstraint.LocalOffsetA.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Hinge axis expressed in body A local space.
    /// </summary>
    public Vector3 LocalHingeAxisA
    {
        get
        {
            return BepuConstraint.LocalHingeAxisA.ToStride();
        }
        set
        {
            BepuConstraint.LocalHingeAxisA = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public Vector3 LocalOffsetB
    {
        get
        {
            return BepuConstraint.LocalOffsetB.ToStride();
        }
        set
        {
            BepuConstraint.LocalOffsetB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <summary>
    /// Hinge axis expressed in body B local space.
    /// </summary>
    public Vector3 LocalHingeAxisB
    {
        get
        {
            return BepuConstraint.LocalHingeAxisB.ToStride();
        }
        set
        {
            BepuConstraint.LocalHingeAxisB = value.ToNumeric();
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public float SpringFrequency
    {
        get
        {
            return BepuConstraint.SpringSettings.Frequency;
        }
        set
        {
            BepuConstraint.SpringSettings.Frequency = value;
            TryUpdateDescription();
        }
    }

    /// <inheritdoc/>
    public float SpringDampingRatio
    {
        get
        {
            return BepuConstraint.SpringSettings.DampingRatio;
        }
        set
        {
            BepuConstraint.SpringSettings.DampingRatio = value;
            TryUpdateDescription();
        }
    }
}
