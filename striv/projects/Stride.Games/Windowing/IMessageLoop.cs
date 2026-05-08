// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.
using System;

namespace Stride.Games
{
    /// <summary>
    /// Message-loop contract for concrete window backends.
    /// </summary>
    /// <remarks>
    /// Loop ownership is backend-specific, while <see cref="GameBase.Tick"/> remains the authoritative lifecycle tick.
    /// This interface is a likely extraction point for a future windowing-focused module.
    /// </remarks>
    public interface IMessageLoop : IDisposable
    {
        bool NextFrame();
    }
}
