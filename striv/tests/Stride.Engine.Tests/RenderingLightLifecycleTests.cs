using Stride.Core;
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
    public void LightRegistrationActuator_RegisterAndUnregisterLight_UpdatesRenderLightLookup()
    {
        var processor = new LightProcessor();
        var component = new LightComponent();
        var renderLight = new RenderLight();

        ((ILightRegistrationActuator)processor).RegisterLight(component, renderLight);

        Assert.Same(renderLight, processor.GetRenderLight(component));

        ((ILightRegistrationActuator)processor).UnregisterLight(component);

        Assert.Same(renderLight, processor.GetRenderLight(component));
    }

    [Fact]
    public void LightRegistrationActuator_UnregisterMissingLight_DoesNotThrow()
    {
        var processor = new LightProcessor();
        var component = new LightComponent();

        var exception = Record.Exception(() => ((ILightRegistrationActuator)processor).UnregisterLight(component));

        Assert.Null(exception);
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
