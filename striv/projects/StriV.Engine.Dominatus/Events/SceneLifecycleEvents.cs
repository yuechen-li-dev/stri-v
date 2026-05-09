using Stride.Engine;

namespace StriV.Engine.Dominatus.Events;

public readonly record struct SceneLoading(string? Name);
public readonly record struct SceneLoaded(Scene Scene);
public readonly record struct SceneUnloading(Scene Scene);
public readonly record struct SceneUnloaded(Scene Scene);

public readonly record struct EntitySceneAttachRequested(Entity Entity, Scene Scene);
public readonly record struct EntitySceneAttached(Entity Entity, Scene Scene);

public readonly record struct EntitySceneDetachRequested(Entity Entity);
public readonly record struct EntitySceneDetached(Entity Entity);
