using Stride.Rendering;
using Xunit;

namespace Stride.Engine.Tests;

public class ModelRenderProcessorLifecycleTests
{
    [Fact]
    public void ModelRenderProcessor_DefaultConstruction_DoesNotRequireGraphicsDevice()
    {
        var processor = new ModelRenderProcessor();

        Assert.NotNull(processor.RenderModels);
        Assert.Empty(processor.RenderModels);
    }

    [Fact]
    public void ModelRenderProcessor_DefaultConstruction_LeavesVisibilityGroupUnset()
    {
        var processor = new ModelRenderProcessor();

        Assert.Null(processor.VisibilityGroup);
    }
}
