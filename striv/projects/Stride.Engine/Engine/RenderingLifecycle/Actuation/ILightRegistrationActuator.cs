using Stride.Rendering.Lights;

namespace Stride.Engine;

internal interface ILightRegistrationActuator
{
    void RegisterLight(LightComponent component, RenderLight renderLight);
    void UpdateLight(LightComponent component, RenderLight renderLight);
    void UnregisterLight(LightComponent component);
}
