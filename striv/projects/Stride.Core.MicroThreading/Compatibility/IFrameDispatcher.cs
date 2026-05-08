// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

namespace Stride.Core.MicroThreading.Compatibility;

/// <summary>
/// Minimal future-facing abstraction for work that must resume on an engine frame boundary.
/// </summary>
/// <remarks>
/// This is a migration seam for replacing legacy MicroThread scheduling. It is intentionally
/// tiny and does not imply that the legacy scheduler is the future implementation.
/// </remarks>
public interface IFrameDispatcher
{
    ValueTask NextFrameAsync(CancellationToken cancellationToken = default);

    ValueTask EnqueueAsync(Func<CancellationToken, ValueTask> work, CancellationToken cancellationToken = default);
}
