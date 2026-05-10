using Stride.Engine.Processors;
using Stride.Rendering.Compositing;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class CameraSlotActuatorTests
{
    [Fact]
    public void CameraSlotActuator_AttachAndClearCamera_UpdatesSlotCamera()
    {
        var actuator = (ICameraSlotActuator)new CameraProcessor();
        var slot = new SceneCameraSlot();
        var camera = new CameraComponent();

        actuator.AttachCamera(slot, camera);
        Assert.Same(camera, slot.Camera);

        actuator.ClearCamera(slot);
        Assert.Null(slot.Camera);
    }
}
