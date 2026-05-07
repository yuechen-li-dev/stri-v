using System.Collections.Generic;

namespace Stride.Input
{
    /// <summary>
    /// Minimal shim to keep InputManager virtual-button APIs compile-time compatible while VirtualButton runtime support is out of scope.
    /// </summary>
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
