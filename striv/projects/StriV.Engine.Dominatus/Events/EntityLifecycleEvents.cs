using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct EntityAttaching(Entity Entity);
public readonly record struct EntityAttached(Entity Entity);
public readonly record struct EntityDetaching(Entity Entity);
public readonly record struct EntityDetached(Entity Entity);
