using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EntityLifecycleParityTests
{
    [Fact]
    public async Task EntityLifecycleParity_LegacyDirectAndDominatusOrchestratedFullCycle_ProduceSameSnapshot()
    {
        var legacy = new EntityLifecycleFixture();
        var orchestrated = new EntityLifecycleFixture();

        EntityLifecycleTestDriver.RunLegacyDirectFullCycle(legacy);
        await EntityLifecycleTestDriver.RunDominatusFullCycleAsync(orchestrated);

        var legacySnapshot = EntityLifecycleTestDriver.CaptureSnapshot(legacy);
        var orchestratedSnapshot = EntityLifecycleTestDriver.CaptureSnapshot(orchestrated);

        Assert.Equal(legacySnapshot, orchestratedSnapshot);
    }
}
