using Stride.Engine.Processors;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class ConcreteProcessorLifecycleTests
{
    [Fact]
    public void CameraProcessor_DefaultConstruction_DoesNotRequireRuntimeServices()
    {
        var processor = new CameraProcessor();
        Assert.NotNull(processor);
    }

    [Fact]
    public void InstancingProcessor_DefaultConstruction_HasValidInertState()
    {
        var processor = new InstancingProcessor();
        Assert.NotNull(processor);
        Assert.Throws<System.InvalidOperationException>(() => _ = processor.VisibilityGroup);
    }

    [Fact]
    public void LightShaftProcessor_DefaultConstruction_DoesNotRequireRuntimeServices()
    {
        var processor = new LightShaftProcessor();
        Assert.NotNull(processor);
        Assert.Throws<System.InvalidOperationException>(() => _ = processor.VisibilityGroup);
    }

    [Fact]
    public void LightShaftBoundingVolumeProcessor_DefaultConstruction_DoesNotRequireRuntimeServices()
    {
        var processor = new LightShaftBoundingVolumeProcessor();
        Assert.NotNull(processor);
    }
}
