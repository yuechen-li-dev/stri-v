// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Input
{
    /// <summary>
    /// An event used when a device was changed
    /// </summary>
    public class DeviceChangedEventArgs : EventArgs
    {
        /// <summary>
        /// The input source this device belongs to
        /// </summary>
        // Populated by InputManager before the event is raised.
        public IInputSource Source = null!;

        /// <summary>
        /// The device that changed
        /// </summary>
        // Populated by InputManager before the event is raised.
        public IInputDevice Device = null!;

        /// <summary>
        /// The type of change that happened
        /// </summary>
        public DeviceChangedEventType Type;
    }
}
