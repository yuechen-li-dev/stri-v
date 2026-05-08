using global::Dominatus.Core.Runtime;
using Xunit;
using StriV.Engine.Dominatus.Actuators;
using StriV.Engine.Dominatus.Blackboard;
using StriV.Engine.Dominatus.Events;
using StriV.Engine.Dominatus.Nodes;

namespace StriV.Engine.Dominatus.Tests;

public sealed class BridgeSurfaceTests
{
    [Fact]
    public void Lifecycle_event_records_can_be_instantiated()
    {
        _ = new EngineStarting();
        _ = new EngineStopping();
        _ = new SceneLoading("BootScene");
    }

    [Fact]
    public void Blackboard_keys_are_defined_with_expected_names()
    {
        Assert.Equal("StriV.Engine.Lifecycle.Phase", EngineBlackboardKeys.EngineLifecyclePhase.Name);
        Assert.Equal("StriV.Scene.Active", EngineBlackboardKeys.ActiveScene.Name);
    }

    [Fact]
    public void Actuator_interfaces_can_be_implemented_by_fake()
    {
        var fake = new FakeActuator();
        Assert.NotNull(fake);
    }

    [Fact]
    public void Node_skeleton_methods_are_accessible()
    {
        Assert.NotNull(typeof(EngineLifecycleNode).GetMethod(nameof(EngineLifecycleNode.Idle)));
        Assert.NotNull(typeof(SceneLifecycleNode).GetMethod(nameof(SceneLifecycleNode.Idle)));
        Assert.NotNull(typeof(EntityAttachmentNode).GetMethod(nameof(EntityAttachmentNode.Idle)));
        Assert.NotNull(typeof(ProcessorLifecycleNode).GetMethod(nameof(ProcessorLifecycleNode.Idle)));
    }

    [Fact]
    public void Dominatus_blackboard_can_set_and_get_bridge_key_types()
    {
        var bb = new global::Dominatus.Core.Blackboard.Blackboard();
        bb.Set(EngineBlackboardKeys.EngineLifecyclePhase, "Starting");

        Assert.True(bb.TryGet(EngineBlackboardKeys.EngineLifecyclePhase, out var phase));
        Assert.Equal("Starting", phase);
    }

    private sealed class FakeActuator : IEngineLifecycleActuator
    {
        public ValueTask StartAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;

        public ValueTask StopAsync(CancellationToken cancellationToken = default) => ValueTask.CompletedTask;
    }
}
