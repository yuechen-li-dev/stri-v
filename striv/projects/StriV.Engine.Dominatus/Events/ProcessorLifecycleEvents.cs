using Dominatus.Core.Runtime;
using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct ProcessorSystemAddRequested(EntityProcessor Processor, EntityManager EntityManager) : IActuationCommand;
public readonly record struct ProcessorSystemAdded(EntityProcessor Processor, EntityManager EntityManager);
public readonly record struct ProcessorSystemRemoveRequested(EntityProcessor Processor, EntityManager EntityManager) : IActuationCommand;
public readonly record struct ProcessorSystemRemoved(EntityProcessor Processor, EntityManager EntityManager);

public readonly record struct ProcessorEntityAddRequested(EntityProcessor Processor, Entity Entity) : IActuationCommand;
public readonly record struct ProcessorEntityAdded(EntityProcessor Processor, Entity Entity);
public readonly record struct ProcessorEntityRemoveRequested(EntityProcessor Processor, Entity Entity) : IActuationCommand;
public readonly record struct ProcessorEntityRemoved(EntityProcessor Processor, Entity Entity);
