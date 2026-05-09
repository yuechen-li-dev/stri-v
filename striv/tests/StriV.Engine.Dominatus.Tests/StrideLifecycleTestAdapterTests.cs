using Stride.Engine;
using StriV.Engine.Dominatus.Tests.Adapters;
using Xunit;

namespace StriV.Engine.Dominatus.Tests;

public sealed class StrideLifecycleTestAdapterTests
{
    [Fact]
    public async Task StrideTransformLifecycleTestAdapter_AttachParent_UsesCurrentStrideParenting()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var adapter = new StrideTransformLifecycleTestAdapter();

        await adapter.AttachParentAsync(child, parent);

        Assert.Equal(1, adapter.AttachCalls);
        Assert.Same(parent.Transform, child.Transform.Parent);
        Assert.Contains(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideTransformLifecycleTestAdapter_DetachParent_UsesCurrentStrideDetach()
    {
        var parent = new Entity("Parent");
        var child = new Entity("Child");
        var adapter = new StrideTransformLifecycleTestAdapter();

        await adapter.AttachParentAsync(child, parent);
        await adapter.DetachParentAsync(child);

        Assert.Equal(1, adapter.DetachCalls);
        Assert.Null(child.Transform.Parent);
        Assert.DoesNotContain(child.Transform, parent.Transform.Children);
    }

    [Fact]
    public async Task StrideSceneLifecycleTestAdapter_AttachEntity_UsesCurrentStrideSceneMembership()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var adapter = new StrideSceneLifecycleTestAdapter();

        await adapter.AttachEntityToSceneAsync(entity, scene);

        Assert.Equal(1, adapter.AttachCalls);
        Assert.Same(scene, entity.Scene);
        Assert.Contains(entity, scene.Entities);
    }

    [Fact]
    public async Task StrideSceneLifecycleTestAdapter_DetachEntity_UsesCurrentStrideSceneDetach()
    {
        var entity = new Entity("Entity");
        var scene = new Scene();
        var adapter = new StrideSceneLifecycleTestAdapter();

        await adapter.AttachEntityToSceneAsync(entity, scene);
        await adapter.DetachEntityFromSceneAsync(entity);

        Assert.Equal(1, adapter.DetachCalls);
        Assert.Null(entity.Scene);
        Assert.DoesNotContain(entity, scene.Entities);
    }
}
