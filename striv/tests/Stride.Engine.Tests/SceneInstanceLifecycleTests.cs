using Stride.Core;
using Xunit;

namespace Stride.Engine.Tests;

public class SceneInstanceLifecycleTests
{
    [Fact]
    public void SceneInstance_RootScene_Set_AddsEntitiesToEntityManager()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var entity = new Entity("Entity");
        rootScene.Entities.Add(entity);

        sceneInstance.RootScene = rootScene;

        Assert.Same(rootScene, sceneInstance.RootScene);
        Assert.Contains(entity, sceneInstance);
    }

    [Fact]
    public void SceneInstance_RootScene_Clear_RemovesEntitiesFromEntityManager()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var entity = new Entity("Entity");
        rootScene.Entities.Add(entity);
        sceneInstance.RootScene = rootScene;

        sceneInstance.RootScene = null;

        Assert.Null(sceneInstance.RootScene);
        Assert.DoesNotContain(entity, sceneInstance);
    }

    [Fact]
    public void SceneInstance_RootSceneChanged_FiresOnSetAndClear()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());
        var rootScene = new Scene();
        var eventCount = 0;
        sceneInstance.RootSceneChanged += (_, _) => eventCount++;

        sceneInstance.RootScene = rootScene;
        sceneInstance.RootScene = null;

        Assert.Equal(2, eventCount);
    }

    [Fact]
    public void SceneInstance_RootScene_Clear_IsIdempotent()
    {
        var sceneInstance = new SceneInstance(new ServiceRegistry());

        var first = Record.Exception(() => sceneInstance.RootScene = null);
        var second = Record.Exception(() => sceneInstance.RootScene = null);

        Assert.Null(first);
        Assert.Null(second);
    }
}
