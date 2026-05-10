// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;

namespace Stride.Engine
{
    internal enum EntityProcessorMembershipChangeKind
    {
        Added,
        Removed,
        Revalidated,
    }

    internal readonly record struct EntityProcessorMembershipChange(
        EntityProcessor Processor,
        Entity Entity,
        EntityComponent Component,
        EntityProcessorMembershipChangeKind Kind)
    {
        public static EntityProcessorMembershipChange Added(EntityProcessor processor, Entity entity, EntityComponent component)
        {
            ArgumentNullException.ThrowIfNull(processor);
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(component);
            return new(processor, entity, component, EntityProcessorMembershipChangeKind.Added);
        }

        public static EntityProcessorMembershipChange Removed(EntityProcessor processor, Entity entity, EntityComponent component)
        {
            ArgumentNullException.ThrowIfNull(processor);
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(component);
            return new(processor, entity, component, EntityProcessorMembershipChangeKind.Removed);
        }

        public static EntityProcessorMembershipChange Revalidated(EntityProcessor processor, Entity entity, EntityComponent component)
        {
            ArgumentNullException.ThrowIfNull(processor);
            ArgumentNullException.ThrowIfNull(entity);
            ArgumentNullException.ThrowIfNull(component);
            return new(processor, entity, component, EntityProcessorMembershipChangeKind.Revalidated);
        }
    }
}
