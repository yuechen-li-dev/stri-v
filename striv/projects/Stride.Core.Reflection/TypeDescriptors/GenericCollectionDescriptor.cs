// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System.Reflection;
using Stride.Core.Yaml.Serialization;

namespace Stride.Core.Reflection;

/// <summary>
/// Provides a descriptor for generic <see cref="ICollection{T}"/> implementations that are neither list, set nor dictionary.
/// </summary>
public class GenericCollectionDescriptor : CollectionDescriptor
{
    private static readonly object[] EmptyObjects = [];
    private static readonly List<string> ListOfMembersToRemove = ["Count", "IsReadOnly"];

    private readonly Func<object, bool> isReadOnlyMethod;
    private readonly Func<object, int> getCollectionCountMethod;
    private readonly Action<object, object?> addMethod;
    private readonly Action<object, object?> removeMethod;
    private readonly Action<object> clearMethod;

    public GenericCollectionDescriptor(ITypeDescriptorFactory factory, Type type, bool emitDefaultValues, IMemberNamingConvention namingConvention)
        : base(factory, type, emitDefaultValues, namingConvention)
    {
        var iCollectionType = type.GetInterface(typeof(ICollection<>))
            ?? throw new ArgumentException("Expecting a type inheriting from System.Collections.Generic.ICollection<T>", nameof(type));

        ElementType = iCollectionType.GetGenericArguments()[0];

        var add = iCollectionType.GetMethod(nameof(ICollection<object>.Add), [ElementType])!;
        var remove = iCollectionType.GetMethod(nameof(ICollection<object>.Remove), [ElementType])!;
        var clear = iCollectionType.GetMethod(nameof(ICollection<object>.Clear), Type.EmptyTypes)!;
        var countMethod = iCollectionType.GetProperty(nameof(ICollection<object>.Count))!.GetGetMethod()!;
        var isReadOnly = iCollectionType.GetProperty(nameof(ICollection<object>.IsReadOnly))!.GetGetMethod()!;

        addMethod = (obj, value) => add.Invoke(obj, [value]);
        removeMethod = (obj, value) => remove.Invoke(obj, [value]);
        clearMethod = obj => clear.Invoke(obj, EmptyObjects);
        getCollectionCountMethod = o => (int)countMethod.Invoke(o, null)!;
        isReadOnlyMethod = obj => (bool)isReadOnly.Invoke(obj, null)!;

        HasAdd = true;
        HasRemove = true;
        HasInsert = false;
        HasRemoveAt = false;
        HasIndexerAccessors = false;
    }

    public override void Initialize(IComparer<object> keyComparer)
    {
        base.Initialize(keyComparer);
        IsPureCollection = Count == 0;
    }

    public override DescriptorCategory Category => DescriptorCategory.Collection;

    public override bool IsReadOnly(object collection)
    {
        return collection == null || isReadOnlyMethod(collection);
    }

    public override object? GetValue(object collection, object index)
    {
        throw new InvalidOperationException($"{nameof(GenericCollectionDescriptor)} does not support indexed access.");
    }

    public override object? GetValue(object collection, int index)
    {
        throw new InvalidOperationException($"{nameof(GenericCollectionDescriptor)} does not support indexed access.");
    }

    public override void SetValue(object list, object index, object? value)
    {
        throw new InvalidOperationException($"{nameof(GenericCollectionDescriptor)} does not support indexed access.");
    }

    public override void Add(object collection, object? value) => addMethod(collection, value);
    public override void Insert(object collection, int index, object? value) => throw new InvalidOperationException($"{nameof(GenericCollectionDescriptor)} should not call function 'Insert'.");
    public override void Remove(object collection, object? item) => removeMethod(collection, item);
    public override void RemoveAt(object collection, int index) => throw new InvalidOperationException($"{nameof(GenericCollectionDescriptor)} should not call function 'RemoveAt'.");
    public override void Clear(object collection) => clearMethod(collection);
    public override int GetCollectionCount(object? collection) => collection == null ? -1 : getCollectionCountMethod(collection);

    protected override bool PrepareMember(MemberDescriptorBase member, MemberInfo metadataClassMemberInfo)
    {
        if (member is PropertyDescriptor && ListOfMembersToRemove.Contains(member.OriginalName))
            return false;

        return !IsCompilerGenerated && base.PrepareMember(member, metadataClassMemberInfo);
    }
}
