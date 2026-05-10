using Stride.Core;
using Stride.Engine;
using Xunit;

namespace Stride.Engine.Tests;

public sealed class EntityManagerProcessorPolicyTests
{
    [Fact]
    public void EntityProcessorMembershipChange_Added_HasExpectedPayload()
    {
        CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        var component = new MainTestComponent();

        var change = EntityProcessorMembershipChange.Added(processor, entity, component);

        Assert.Same(processor, change.Processor);
        Assert.Same(entity, change.Entity);
        Assert.Same(component, change.Component);
        Assert.Equal(EntityProcessorMembershipChangeKind.Added, change.Kind);
    }

    [Fact]
    public void EntityProcessorMembershipChange_Removed_HasExpectedPayload()
    {
        CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        var component = new MainTestComponent();

        var change = EntityProcessorMembershipChange.Removed(processor, entity, component);

        Assert.Same(processor, change.Processor);
        Assert.Same(entity, change.Entity);
        Assert.Same(component, change.Component);
        Assert.Equal(EntityProcessorMembershipChangeKind.Removed, change.Kind);
    }

    [Fact]
    public void EntityProcessorMembershipChange_Revalidated_HasExpectedPayload()
    {
        CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        var component = new MainTestComponent();

        var change = EntityProcessorMembershipChange.Revalidated(processor, entity, component);

        Assert.Same(processor, change.Processor);
        Assert.Same(entity, change.Entity);
        Assert.Same(component, change.Component);
        Assert.Equal(EntityProcessorMembershipChangeKind.Revalidated, change.Kind);
    }

    [Fact]
    public void EntityManager_RequiredTypeProcessor_DoesNotMatchUntilAllRequiredComponentsPresent()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        entity.Components.Add(new MainTestComponent());

        Assert.Equal(0, processor.AddedCount);
    }

    [Fact]
    public void EntityManager_RequiredTypeProcessor_AddsMembershipWhenRequiredComponentAppears()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        var mainComponent = new MainTestComponent();
        entity.Components.Add(mainComponent);
        entity.Components.Add(new RequiredTestComponent());

        Assert.Equal(1, processor.AddedCount);
        Assert.Same(mainComponent, Assert.Single(processor.AddedComponents));
    }

    [Fact]
    public void EntityManager_RequiredTypeProcessor_RemovesMembershipWhenRequiredComponentDisappears()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        var mainComponent = new MainTestComponent();
        var requiredComponent = new RequiredTestComponent();
        entity.Components.Add(mainComponent);
        entity.Components.Add(requiredComponent);

        Assert.Equal(1, processor.AddedCount);

        entity.Components.Remove(requiredComponent);

        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(mainComponent, Assert.Single(processor.RemovedComponents));
    }

    [Fact]
    public void EntityManager_RequiredTypeProcessor_RevalidatesOnRequiredComponentRemoveAdd()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        var originalMainComponent = new MainTestComponent();
        var requiredComponent = new RequiredTestComponent();
        entity.Components.Add(originalMainComponent);
        entity.Components.Add(requiredComponent);

        Assert.Equal(1, processor.AddedCount);

        entity.Components.Remove(requiredComponent);
        entity.Components.Add(new RequiredTestComponent());

        Assert.Equal(2, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.Same(originalMainComponent, processor.RemovedComponents[0]);
        Assert.Same(originalMainComponent, processor.AddedComponents[1]);
    }

    [Fact]
    public void EntityManager_ComponentAdded_RoutesProcessorMembershipAddedOnce()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        entity.Components.Add(new RequiredTestComponent());
        entity.Components.Add(new MainTestComponent());

        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(["add"], processor.MembershipTransitions);
    }

    [Fact]
    public void EntityManager_ComponentRemoved_RoutesProcessorMembershipRemovedOnce()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        var main = new MainTestComponent();
        var required = new RequiredTestComponent();
        entity.Components.Add(main);
        entity.Components.Add(required);
        entity.Components.Remove(required);

        Assert.Equal(1, processor.RemovedCount);
        Assert.Equal(["add", "remove"], processor.MembershipTransitions);
    }

    [Fact]
    public void EntityManager_RequiredTypeProcessor_RevalidationPreservesExpectedOrder()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new RequiredTypeRecordingProcessor();
        manager.Processors.Add(processor);

        var main = new MainTestComponent();
        var required = new RequiredTestComponent();
        entity.Components.Add(main);
        entity.Components.Add(required);
        entity.Components.Remove(required);
        entity.Components.Add(new RequiredTestComponent());

        Assert.Equal(["add", "remove", "add"], processor.MembershipTransitions);
    }

    [Fact]
    public void EntityProcessor_AssociatedData_AddRemoveLifecycle_PassesNonNullDataWhenMatched()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: true);
        var processor = new AssociatedDataRecordingProcessor();
        manager.Processors.Add(processor);

        manager.AddEntityToProcessor(processor, entity);
        manager.RemoveEntityFromProcessor(processor, entity);

        Assert.Equal(1, processor.AddedCount);
        Assert.Equal(1, processor.RemovedCount);
        Assert.NotNull(processor.LastAddedData);
        Assert.NotNull(processor.LastRemovedData);
        Assert.Equal(processor.LastAddedData, processor.LastRemovedData);
    }

    [Fact]
    public void EntityProcessor_AssociatedData_RemoveWithoutPriorAdd_DoesNotNullReference()
    {
        var manager = CreateManagerWithEntity(out var entity, withComponent: false);
        var processor = new AssociatedDataRecordingProcessor();
        manager.Processors.Add(processor);

        var exception = Record.Exception(() => manager.RemoveEntityFromProcessor(processor, entity));

        Assert.Null(exception);
        Assert.Equal(0, processor.RemovedCount);
    }

    private static SceneInstance CreateManagerWithEntity(out Entity entity, bool withComponent)
    {
        var manager = new SceneInstance(new ServiceRegistry()) { RootScene = new Scene() };
        entity = new Entity("Entity");
        if (withComponent)
        {
            entity.Components.Add(new TestComponent());
        }

        manager.RootScene.Entities.Add(entity);
        return manager;
    }

    private sealed class TestComponent : EntityComponent;
    private sealed class MainTestComponent : EntityComponent;
    private sealed class RequiredTestComponent : EntityComponent;

    private sealed class AssociatedDataRecordingProcessor : EntityProcessor<TestComponent, string>
    {
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public string? LastAddedData { get; private set; }
        public string? LastRemovedData { get; private set; }

        protected override string GenerateComponentData(Entity entity, TestComponent component) => $"{entity.Name}:{component.GetType().Name}";

        protected override void OnEntityComponentAdding(Entity entity, TestComponent component, string data)
        {
            AddedCount++;
            LastAddedData = data;
        }

        protected override void OnEntityComponentRemoved(Entity entity, TestComponent component, string data)
        {
            RemovedCount++;
            LastRemovedData = data;
        }
    }

    private sealed class RequiredTypeRecordingProcessor : EntityProcessor<MainTestComponent, string>
    {
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public List<MainTestComponent> AddedComponents { get; } = [];
        public List<MainTestComponent> RemovedComponents { get; } = [];
        public List<string> MembershipTransitions { get; } = [];

        public RequiredTypeRecordingProcessor()
            : base(typeof(RequiredTestComponent))
        {
        }

        protected override string GenerateComponentData(Entity entity, MainTestComponent component) => entity.Name;

        protected override void OnEntityComponentAdding(Entity entity, MainTestComponent component, string data)
        {
            AddedCount++;
            AddedComponents.Add(component);
            MembershipTransitions.Add("add");
        }

        protected override void OnEntityComponentRemoved(Entity entity, MainTestComponent component, string data)
        {
            RemovedCount++;
            RemovedComponents.Add(component);
            MembershipTransitions.Add("remove");
        }
    }
}
