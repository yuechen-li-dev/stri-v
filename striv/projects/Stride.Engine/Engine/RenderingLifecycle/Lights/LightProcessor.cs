// Copyright (c) .NET Foundation and Contributors (https://dotnetfoundation.org/ & https://stride3d.net) and Silicon Studio Corp. (https://www.siliconstudio.co.jp)
// Distributed under the MIT license. See the LICENSE.md file in the project root for more information.

using System;
using Stride.Core;
using Stride.Core.Mathematics;
using Stride.Engine;

namespace Stride.Rendering.Lights
{
    /// <summary>
    /// Process <see cref="LightComponent"/> stored in an <see cref="EntityManager"/> by providing grouped lights per types/shadows.
    /// </summary>
    public class LightProcessor : EntityProcessor<LightComponent, RenderLight>, IEntityComponentRenderProcessor, ILightRegistrationActuator
    {
        /// <summary>
        /// The default direction of a light vector is (x,y,z) = (0,0,-1)
        /// </summary>
        public static readonly Vector3 DefaultDirection = new Vector3(0, 0, -1);

        private const int DefaultLightCapacityCount = 512;

        /// <summary>
        /// Initializes a new instance of the <see cref="LightProcessor"/> class.
        /// </summary>
        public LightProcessor()
        {
        }

        /// <inheritdoc/>
        public VisibilityGroup VisibilityGroup { get; set; } = null!;

        /// <summary>
        /// Gets the active lights.
        /// </summary>
        /// <value>The lights.</value>
        public RenderLightCollection Lights { get; } = new RenderLightCollection(DefaultLightCapacityCount);

        public RenderLight? GetRenderLight(LightComponent lightComponent)
        {
            ComponentDatas.TryGetValue(lightComponent, out var renderLight);
            return renderLight;
        }

        protected override RenderLight GenerateComponentData(Entity entity, LightComponent component) => new RenderLight();

        protected override bool IsAssociatedDataValid(Entity entity, LightComponent component, RenderLight associatedData) => true;

        protected internal override void OnSystemAdd()
        {
            base.OnSystemAdd();
            var visibilityGroup = VisibilityGroup ?? throw new InvalidOperationException("LightProcessor requires an initialized visibility group before system add.");
            visibilityGroup.Tags.Set(ForwardLightingRenderFeature.CurrentLights, Lights);
        }

        protected internal override void OnSystemRemove()
        {
            VisibilityGroup?.Tags.Remove(ForwardLightingRenderFeature.CurrentLights);
            base.OnSystemRemove();
        }


        protected override void OnEntityComponentAdding(Entity entity, LightComponent component, RenderLight data)
        {
            RegisterLight(component, data);
            base.OnEntityComponentAdding(entity, component, data);
        }

        protected override void OnEntityComponentRemoved(Entity entity, LightComponent component, RenderLight data)
        {
            UnregisterLight(component);
            base.OnEntityComponentRemoved(entity, component, data);
        }

        public void RegisterLight(LightComponent component, RenderLight renderLight)
        {
            if (!ComponentDatas.ContainsKey(component))
            {
                ComponentDatas.Add(component, renderLight);
            }
        }

        public void UpdateLight(LightComponent component, RenderLight renderLight)
        {
            renderLight.Type = component.Type;
            renderLight.Intensity = component.Intensity;
            renderLight.WorldMatrix = component.Entity.Transform.WorldMatrix;
        }

        public void UnregisterLight(LightComponent component)
        {
            if (ComponentDatas.TryGetValue(component, out var renderLight))
            {
                Lights.Remove(renderLight);
            }
        }

        public override void Draw(RenderContext context)
        {
            // 1) Clear the cache of current lights (without destroying collections but keeping previously allocated ones)
            Lights.Clear();

            var colorSpace = context.GraphicsDevice.ColorSpace;

            // 2) Prepare lights to be dispatched to the correct light group
            foreach (var lightPair in ComponentDatas)
            {
                var lightComponent = lightPair.Key;
                var renderLight = lightPair.Value;

                if (lightComponent.Type == null || !lightComponent.Enabled)
                {
                    continue;
                }

                UpdateLight(lightComponent, renderLight);

                // Update info specific to this light type
                if (!renderLight.Type.Update(renderLight))
                {
                    continue;
                }

                // Compute light direction and position
                Vector3 lightDirection;
                var lightDir = DefaultDirection;
                Vector3.TransformNormal(ref lightDir, ref renderLight.WorldMatrix, out lightDirection);
                lightDirection.Normalize();

                renderLight.Position = renderLight.WorldMatrix.TranslationVector;
                renderLight.Direction = lightDirection;

                // Color
                var colorLight = renderLight.Type as IColorLight;
                renderLight.Color = colorLight?.ComputeColor(colorSpace, renderLight.Intensity) ?? new Color3();

                // Compute bounding boxes
                renderLight.UpdateBoundingBox();

                Lights.Add(renderLight);
            }
        }
    }
}
