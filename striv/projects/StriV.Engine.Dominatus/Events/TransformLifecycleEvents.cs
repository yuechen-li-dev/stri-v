using Dominatus.Core.Runtime;
using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct TransformParentAttachRequested(Entity Child, Entity Parent) : IActuationCommand;
public readonly record struct TransformParentAttached(Entity Child, Entity Parent);
public readonly record struct TransformParentDetachRequested(Entity Child) : IActuationCommand;
public readonly record struct TransformParentDetached(Entity Child);
