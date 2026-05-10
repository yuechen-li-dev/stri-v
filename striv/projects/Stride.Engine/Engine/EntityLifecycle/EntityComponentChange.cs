// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine
{
    public enum EntityComponentChangeKind
    {
        Added,
        Removed,
        Replaced,
    }

    public readonly record struct EntityComponentChange(
        Entity Entity,
        EntityComponent? OldComponent,
        EntityComponent? NewComponent,
        EntityComponentChangeKind Kind)
    {
        public EntityComponent AddedComponent => NewComponent
            ?? throw new InvalidOperationException("Component change does not contain an added component.");

        public EntityComponent RemovedComponent => OldComponent
            ?? throw new InvalidOperationException("Component change does not contain a removed component.");

        public static EntityComponentChange Added(Entity entity, EntityComponent component) => new(entity, null, component, EntityComponentChangeKind.Added);

        public static EntityComponentChange Removed(Entity entity, EntityComponent component) => new(entity, component, null, EntityComponentChangeKind.Removed);

        public static EntityComponentChange Replaced(Entity entity, EntityComponent oldComponent, EntityComponent newComponent) => new(entity, oldComponent, newComponent, EntityComponentChangeKind.Replaced);
    }
}
