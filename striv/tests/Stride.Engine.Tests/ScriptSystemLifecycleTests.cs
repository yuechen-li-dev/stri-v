using System;
using Stride.Core;
using Stride.Engine.Processors;
using Xunit;

namespace Stride.Engine.Tests;

public class ScriptSystemLifecycleTests
{
    [Fact]
    public void ScriptSystem_Scheduler_IsAvailableBeforeDestroy()
    {
        var scriptSystem = new ScriptSystem(new ServiceRegistry());

        Assert.NotNull(scriptSystem.Scheduler);
    }

    [Fact]
    public void ScriptSystem_Destroy_IsIdempotent()
    {
        var scriptSystem = new ScriptSystem(new ServiceRegistry());

        var firstDispose = Record.Exception(scriptSystem.Dispose);
        var secondDispose = Record.Exception(scriptSystem.Dispose);

        Assert.Null(firstDispose);
        Assert.Null(secondDispose);
    }

    [Fact]
    public void ScriptSystem_Scheduler_AfterDestroy_ThrowsObjectDisposedException()
    {
        var scriptSystem = new ScriptSystem(new ServiceRegistry());

        scriptSystem.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = scriptSystem.Scheduler);
    }
}
