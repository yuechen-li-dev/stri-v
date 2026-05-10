using Stride.Engine;
using Stride.Rendering.LightProbes;
using Stride.Rendering.Lights;
using System;
using System.Collections.Generic;
using Xunit;

namespace Stride.Engine.Tests;

public class RenderingLightLifecycleTests
{
    [Fact]
    public void LightProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice()
    {
        var processor = new LightProcessor();

        Assert.Null(processor.VisibilityGroup);
        Assert.NotNull(processor.Lights);
        Assert.Empty(processor.Lights);
    }

    [Fact]
    public void LightProbeProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice()
    {
        var processor = new LightProbeProcessor();

        Assert.Null(processor.VisibilityGroup);
    }

    [Fact]
    public void LightProbeComponent_DefaultConstruction_HasValidInertState()
    {
        var component = new LightProbeComponent();

        Assert.NotNull(component.Coefficients);
        Assert.Empty(component.Coefficients);
    }

    [Fact]
    public void LightProbeGenerator_UnboundProbeComponent_ThrowsInvalidOperationException()
    {
        var probes = new List<LightProbeComponent>
        {
            new(),
            new(),
            new(),
            new(),
        };

        var exception = Assert.Throws<InvalidOperationException>(() => LightProbeGenerator.GenerateRuntimeData(probes));
        Assert.Contains("attached to an entity", exception.Message, StringComparison.Ordinal);
    }
}
