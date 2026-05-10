namespace Stride.Engine;

internal interface IModelNodeLinkActuator
{
    void AttachModelNodeLink(TransformComponent transformComponent, ModelNodeTransformLink link);

    void ClearModelNodeLink(TransformComponent transformComponent);
}
