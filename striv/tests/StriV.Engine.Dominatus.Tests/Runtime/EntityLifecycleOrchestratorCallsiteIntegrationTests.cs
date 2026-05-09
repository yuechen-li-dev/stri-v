using Xunit;

namespace StriV.Engine.Dominatus.Tests.Runtime;

public sealed class EntityLifecycleOrchestratorCallsiteIntegrationTests
{
    [Fact]
    public async Task EntityLifecycleDefaultPath_FullCycle_UsesDominatusOrchestratorThroughEngineCallsite()
    {
        var fixture = new EntityLifecycleFixture();

        await EntityLifecycleTestDriver.RunDominatusFullCycleAsync(fixture);

        var snapshot = EntityLifecycleTestDriver.CaptureSnapshot(fixture);

        Assert.True(snapshot.ParentSceneDetached);
        Assert.True(snapshot.ChildSceneDetached);
        Assert.True(snapshot.ChildTransformDetached);
        Assert.True(snapshot.ParentChildrenDoesNotContainChild);
        Assert.True(snapshot.ProcessorDetached);
        Assert.True(snapshot.ManagerDoesNotContainProcessor);
        Assert.Equal(1, snapshot.ProcessorAddedCount);
        Assert.Equal(1, snapshot.ProcessorRemovedCount);
        Assert.Equal("Child", snapshot.AddedEntityName);
        Assert.Equal("Child", snapshot.RemovedEntityName);
    }
}
