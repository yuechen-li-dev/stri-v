// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using Stride.BepuPhysics.Definitions;
using Stride.Core;
using Stride.Games;

namespace Stride.BepuPhysics.Systems;

/// <summary>
/// Updates every registered <see cref="BepuSimulation"/> once per Stride frame using warped elapsed time.
/// </summary>
internal class PhysicsGameSystem : GameSystemBase
{
    // This configuration owns the simulation list used by all runtime processors in the scene.
    private BepuConfiguration _bepuConfiguration;

    public PhysicsGameSystem(BepuConfiguration configuration, IServiceRegistry registry) : base(registry)
    {
        _bepuConfiguration = configuration;
        UpdateOrder = SystemsOrderHelper.ORDER_OF_GAME_SYSTEM;
        Enabled = true; // Enabled by default.

        foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
        {
            // Reset soft-start as soon as the system is created so startup behavior is deterministic.
            bepuSim.ResetSoftStart();
        }
    }

    /// <summary>
    /// Advances each configured simulation for the current frame when positive warped time elapsed.
    /// </summary>
    public override void Update(GameTime time)
    {
        var elapsed = time.WarpElapsed;
        if (elapsed <= TimeSpan.Zero)
            return;

        foreach (var bepuSim in _bepuConfiguration.BepuSimulations)
        {
            bepuSim.Update(elapsed);
        }
    }
}
