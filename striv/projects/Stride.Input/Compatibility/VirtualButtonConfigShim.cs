using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// Compatibility-only shim to preserve public virtual-button API surface.
    /// </summary>
    /// <remarks>
    /// Stri-V currently has no active VirtualButton runtime subsystem in <c>Stride.Input</c>.
    /// This type exists to keep build/API compatibility after the VirtualButton cull.
    /// Do not expand this shim into a new runtime subsystem without an explicit product decision.
    /// </remarks>
    public class VirtualButtonConfigSet : List<VirtualButtonConfig>
    {
    }

    /// <summary>
    /// Minimal virtual-button config shim for desktop-focused Stri-V input builds.
    /// </summary>
    public class VirtualButtonConfig
    {
        public IEnumerable<object> BindingNames => System.Array.Empty<object>();

        public virtual float GetValue(InputManager manager, object bindingName) => 0.0f;

        public virtual bool IsPressed(InputManager manager, object bindingName) => false;

        public virtual bool IsDown(InputManager manager, object bindingName) => false;

        public virtual bool IsReleased(InputManager manager, object bindingName) => false;
    }
}
