namespace Stride.Engine;

internal interface ITransformHierarchyActuator
{
    void AttachParent(TransformComponent child, TransformComponent parent);
    void DetachParent(TransformComponent child);
}
