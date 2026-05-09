using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct EntityCloneRequested(Entity Source);
public readonly record struct EntityCloneCompleted(Entity Source, Entity ClonedEntity);
