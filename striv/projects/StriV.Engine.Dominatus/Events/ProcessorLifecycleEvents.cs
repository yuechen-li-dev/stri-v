using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct ProcessorAdding(EntityProcessor Processor);
public readonly record struct ProcessorAdded(EntityProcessor Processor);
public readonly record struct ProcessorRemoving(EntityProcessor Processor);
public readonly record struct ProcessorRemoved(EntityProcessor Processor);
