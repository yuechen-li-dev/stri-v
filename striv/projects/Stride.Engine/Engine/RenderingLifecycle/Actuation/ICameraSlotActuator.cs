using Stride.Rendering.Compositing;

namespace Stride.Engine;

internal interface ICameraSlotActuator
{
    void AttachCamera(SceneCameraSlot slot, CameraComponent camera);

    void ClearCamera(SceneCameraSlot slot);
}
