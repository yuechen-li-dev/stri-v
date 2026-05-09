using Stride.Core;
using Xunit;

namespace Stride.Engine.Tests;

public class SceneSystemLifecycleTests
{
    [Fact]
    public void SceneSystem_Constructed_HasNoActiveSceneInstanceBeforeInitialization()
    {
        var sceneSystem = new SceneSystem(new ServiceRegistry());

        Assert.Null(sceneSystem.SceneInstance);
    }

    [Fact]
    public void SceneSystem_GraphicsCompositor_CanBeCleared()
    {
        var sceneSystem = new SceneSystem(new ServiceRegistry());

        Assert.NotNull(sceneSystem.GraphicsCompositor);
        sceneSystem.GraphicsCompositor = null;

        Assert.Null(sceneSystem.GraphicsCompositor);
    }
}
