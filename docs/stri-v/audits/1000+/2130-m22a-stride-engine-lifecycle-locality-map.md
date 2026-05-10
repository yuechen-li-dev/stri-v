# 2130 — M22a Stride.Engine lifecycle-locality map

## 1) Files changed

- `docs/stri-v/stride-engine-lifecycle-locality-map.md`
- `docs/stri-v/audits/1000+/2130-m22a-stride-engine-lifecycle-locality-map.md` (this report)

## 2) Task scope

M22a delivers a **Sort / Set-in-order lifecycle-locality map** for `striv/projects/Stride.Engine`.

In scope:
- inventory all `.cs` files;
- classify each file into a proposed lifecycle-locality group;
- identify shared and quarantine candidates;
- define move rules for M22b+;
- recommend first low-risk physical move.

Out of scope (not performed):
- no physical file moves;
- no namespace changes;
- no behavior changes;
- no nullability cleanup;
- no Dominatus migration changes.

## 3) Doctrine

- **Subsystem = module of lifecycle locality**: files whose state/init/teardown/mutation/warning cascades belong together.
- **Shared doctrine**: only true cross-cutting primitives/contracts/base abstractions; never a dump for unknown ownership.
- **Quarantine doctrine**: legacy/excluded/platform/tooling residues with explicit evidence; keep for traceability, do not delete during move passes.
- **NeedsAudit doctrine**: use when ownership is unclear; do not coerce ambiguous files into Shared.

## 4) Project inventory

Commands used:
- `find striv/projects/Stride.Engine -type f -name '*.cs' | sed 's#striv/projects/Stride.Engine/##' | sort > /tmp/striv-m22a-engine-files.txt`
- `wc -l /tmp/striv-m22a-engine-files.txt`
- `awk -F/ '{print $1}' /tmp/striv-m22a-engine-files.txt | sort | uniq -c | sort -nr`
- `sed -n '1,320p' striv/projects/Stride.Engine/Stride.Engine.csproj`

Results:
- `.cs` files: **226**.
- top roots by count: `Engine` (113), `Animations` (44), `Rendering` (26), `Updater` (24), `Shaders.Compiler` (7), `Profiling` (5), `Audio` (4).
- important explicit csproj compile removals:
  - `Audio/*.cs`
  - `Shaders.Compiler/**/*.cs`
  - `Engine/AudioEmitterComponent.cs`
  - `Engine/AudioListenerComponent.cs`
  - `Rendering/Compositing/EditorTopLevelCompositor.cs`
  - `Rendering/Compositing/ForwardRenderer.VRUtils.cs`
  - `Rendering/Compositing/VRDeviceDescription.cs`
  - `Rendering/Compositing/VROverlayRenderer.cs`
  - `Rendering/Compositing/VRRendererSettings.cs`

## 5) Proposed lifecycle groups

- **EntityLifecycle**: entity/component identity and ownership, entity manager and processor coordination, transform graph state.
- **SceneLifecycle**: root scene and scene-instance lifecycle transitions.
- **ScriptLifecycle**: script component execution and scheduler-related lifecycle.
- **CloneLifecycle**: clone serializer and entity clone graph flow.
- **RenderingLifecycle**: render processors/compositor/model/instancing and render registration lifecycle.
- **GameLifecycle**: game bootstrap/runtime systems/services.
- **AnimationLifecycle**: animation evaluators/updater runtime flow.
- **DiagnosticsProfilingLifecycle**: diagnostics/profiling emitters/systems.
- **UpdaterReflection**: reflection/update-engine infrastructure.
- **Shared**: cross-cutting contracts/utilities.
- **Quarantine**: compile-excluded or legacy/platform/tooling surfaces.
- **NeedsAudit**: unresolved ownership.

## 6) Full file map

| Current path | Proposed group | Confidence | Reason | Move later? | Compile status |
| --- | --- | --- | --- | --- | --- |
| `Animations/AnimationBlendOperation.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationBlender.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationChannel.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationClip.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationClipEvaluator.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationClipResult.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectBlittableGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectFloatGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectQuaternionGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectVector3Group.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorDirectVector4Group.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedBlittableGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedFloatGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedIntGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedQuaternionGroup.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedVector3Group.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveEvaluatorOptimizedVector4Group.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationCurveInterpolationType.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationData.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationKeyFrame.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationKeyTangentType.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationKeyValuePairArraySerializer.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationOperation.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationOperationType.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationProcessor.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationRepeatMode.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/AnimationUpdater.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/CompressedTimeSpan.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeAnimationCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeBinaryCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeConstCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeCurveContracts.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeCurveSampler.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeFunctionCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeSeparateCurveVector3.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/ComputeSeparateCurveVector4.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/IComputeCurve.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/Interpolator.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/KeyFrameData.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Animations/PlayingAnimation.cs` | AnimationLifecycle | Medium | Animation runtime/update and curve evaluation | Yes | Included in csproj |
| `Audio/AudioEmitterProcessor.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Audio/AudioEmitterSoundController.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Audio/AudioListenerProcessor.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Audio/AudioSystem.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Engine/ActivableEntityComponent.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/AllowMultipleComponentsAttribute.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/AnimationComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/AsyncScript.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/AudioEmitterComponent.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Engine/AudioListenerComponent.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Engine/BackgroundComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/CameraComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ComponentCategoryAttribute.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/ComponentOrderAttribute.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Design/CloneEntityComponentData.cs` | CloneLifecycle | High | Clone graph/serialization lifecycle | Yes | Included in csproj |
| `Engine/Design/CloneEntityComponentSerializer.cs` | CloneLifecycle | High | Clone graph/serialization lifecycle | Yes | Included in csproj |
| `Engine/Design/CloneSerializer.cs` | CloneLifecycle | High | Clone graph/serialization lifecycle | Yes | Included in csproj |
| `Engine/Design/DefaultEntityComponentProcessorAttribute.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/DefaultEntityComponentRendererAttribute.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/EffectCompilationMode.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/EntityChildPropertyResolver.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/EntityCloner.cs` | CloneLifecycle | High | Clone graph/serialization lifecycle | Yes | Included in csproj |
| `Engine/Design/EntityComponentEventArgs.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/EntityComponentProperty.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/EntityComponentPropertyType.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/ExecutionMode.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/GameSettings.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Engine/Design/IGameSettingsService.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Engine/Design/ParameterCollectionResolver.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Design/ParameterContainerExtensions.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Entity.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityComponent.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityComponentAttributeBase.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityComponentAttributes.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityComponentCollection.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityExtensions.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityManager.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityProcessorCollection.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/EntityTransformExtensions.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Events/EventKey.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Events/EventKeyBase.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Events/EventReceiver.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Events/EventReceiverBase.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Events/EventReceiverOptions.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Events/EventTaskScheduler.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/FlexibleProcessing/IComponent.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/FlexibleProcessing/IDrawProcessor.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/FlexibleProcessing/IProcessorBase.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/FlexibleProcessing/IUpdateProcessor.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/FlexibleProcessing/ProcessorManager.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Game.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Engine/GameSystem.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Engine/Gizmos/GizmoComponentAttribute.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Gizmos/IEntityGizmo.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/Gizmos/IGizmo.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Engine/IInstancing.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ISceneRendererContext.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/InputSystem.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Engine/InstanceComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/InstancingComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/InstancingEntityTransform.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/InstancingUserArray.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/InstancingUserBuffer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/Lifecycle/IEntityLifecycleOrchestrator.cs` | Shared | High | Cross-lifecycle orchestrator contract | Yes | Included in csproj |
| `Engine/LightComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/LightComponentExtensions.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/LightProbeComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/LightShaftBoundingVolumeComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/LightShaftComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ModelComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ModelNodeLinkComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ModelNodeTransformLink.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/ModelViewHierarchyTransformOperation.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/Network/ClientRouterMessage.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/ExceptionMessage.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/RouterClient.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/SimpleSocket.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/SimpleSocketException.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/SocketExtensions.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/SocketMessage.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/SocketMessageLayer.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Implementation.NET/CommsInterface.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Implementation.NET/CommsInterfaceNative.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Implementation.NET/NetworkExtensions.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Implementation.NET/TcpSocketClient.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Implementation.NET/TcpSocketListener.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Plugin.Abstractions/Enums/CommsInterfaceStatus.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Plugin.Abstractions/EventArgs/TcpSocketListenerConnectEventArgs.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Plugin.Abstractions/ICommsInterface.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Plugin.Abstractions/ITcpSocketClient.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/Network/Sockets.Plugin.Abstractions/ITcpSocketListener.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Included in csproj |
| `Engine/OpaqueComponentId.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Prefab.cs` | SceneLifecycle | High | Scene/root scene activation and membership lifecycle | Yes | Included in csproj |
| `Engine/Processors/BackgroundComponentProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/CameraProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/CameraProjectionMode.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/InstanceProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/InstancingProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/LightShaftBoundingVolumeProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/LightShaftProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/ModelNodeLinkProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/ModelTransformProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Processors/ScriptProcessor.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/Processors/ScriptSystem.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/Processors/TransformProcessor.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/RequireComponentAttribute.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/Scene.cs` | SceneLifecycle | High | Scene/root scene activation and membership lifecycle | Yes | Included in csproj |
| `Engine/SceneInstance.cs` | SceneLifecycle | High | Scene/root scene activation and membership lifecycle | Yes | Included in csproj |
| `Engine/SceneSystem.cs` | SceneLifecycle | High | Scene/root scene activation and membership lifecycle | Yes | Included in csproj |
| `Engine/ScriptComponent.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/SpriteComponent.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Engine/StartupScript.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/SyncScript.cs` | ScriptLifecycle | High | Script scheduling/execution lifecycle | Yes | Included in csproj |
| `Engine/TransformComponent.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/TransformLink.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Engine/TransformOperation.cs` | EntityLifecycle | High | Entity/component ownership and processor coordination | Yes | Included in csproj |
| `Internals/LambdaReadOnlyCollection.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Module.cs` | GameLifecycle | Medium | Game bootstrap/services/update loop | Yes | Included in csproj |
| `Profiling/DebugTextSystem.cs` | DiagnosticsProfilingLifecycle | High | Profiling and diagnostics runtime services | Yes | Included in csproj |
| `Profiling/GCProfiling.cs` | DiagnosticsProfilingLifecycle | High | Profiling and diagnostics runtime services | Yes | Included in csproj |
| `Profiling/GameProfilingResults.cs` | DiagnosticsProfilingLifecycle | High | Profiling and diagnostics runtime services | Yes | Included in csproj |
| `Profiling/GameProfilingSorting.cs` | DiagnosticsProfilingLifecycle | High | Profiling and diagnostics runtime services | Yes | Included in csproj |
| `Profiling/GameProfilingSystem.cs` | DiagnosticsProfilingLifecycle | High | Profiling and diagnostics runtime services | Yes | Included in csproj |
| `Properties/AssemblyInfo.cs` | Shared | Medium | Shared infrastructure used across lifecycles | Yes | Included in csproj |
| `Rendering/Background/BackgroundRenderProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/CameraComponentRendererExtensions.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/EditorTopLevelCompositor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Excluded in csproj |
| `Rendering/Compositing/ForwardRenderer.LightProbes.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/ForwardRenderer.VRUtils.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Rendering/Compositing/ForwardRenderer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/GraphicsCompositor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/GraphicsCompositorHelper.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/SceneCameraRenderer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/SceneCameraSlot.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/SceneCameraSlotCollection.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/SceneCameraSlotId.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/SceneExternalCameraRenderer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Compositing/VRDeviceDescription.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Rendering/Compositing/VROverlayRenderer.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Rendering/Compositing/VRRendererSettings.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Rendering/IEntityComponentRenderProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/LightProbes/LightProbeGenerator.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/LightProbes/LightProbeProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Lights/LightProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/ModelRenderProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Skyboxes/CubemapFromTextureRenderer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Skyboxes/CubemapRendererBase.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Skyboxes/CubemapSceneRenderer.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Sprites/SpriteAnimationSystem.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Rendering/Sprites/SpriteRenderProcessor.cs` | RenderingLifecycle | High | Render/model/instancing/compositor registration lifecycle | Yes | Included in csproj |
| `Shaders.Compiler/EffectCompilerFactory.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/Internals/NetworkVirtualFileProvider.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/RemoteEffectCompiler.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/RemoteEffectCompilerClient.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/RemoteEffectCompilerEffectAnswer.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/RemoteEffectCompilerEffectRequest.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Shaders.Compiler/RemoteEffectCompilerEffectRequested.cs` | Quarantine | High | Legacy/excluded or platform/tooling residue | Yes | Excluded in csproj |
| `Updater/ArrayUpdateResolver.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/BlittableHelper.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/CompiledUpdate.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/DataMemberUpdatableAttribute.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/EnterChecker.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/ListEnterChecker.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/ListUpdateResolver.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableArrayAccessor.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableCustomAccessor.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableField.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableFieldT.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableListAccessor.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableMember.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatableProperty.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatablePropertyBase.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatablePropertyObject.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdatablePropertyT.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateEngine.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateEngineHelper.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateMemberInfo.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateMemberResolver.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateObjectData.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateOperation.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |
| `Updater/UpdateOperationType.cs` | UpdaterReflection | High | Reflection/update engine machinery | Yes | Included in csproj |

## 7) Cross-subsystem/shared analysis

| File | Used by | Why shared/cross-cutting | Proposed handling |
| --- | --- | --- | --- |
| `Engine/Entity.cs` | Scene, Script, Rendering, Clone | Base identity container for most runtime subsystems | Keep stable in `EntityLifecycle`; avoid early move churn. |
| `Engine/EntityComponent.cs` | Scene, Script, Rendering, Animation | Common component abstraction and metadata anchor | Keep as `EntityLifecycle` core abstraction. |
| `Engine/EntityManager.cs` | SceneSystem, ScriptSystem, processors, lifecycle orchestrator | Central add/remove/order lifecycle coordinator | Keep in `EntityLifecycle`; defer deeper split until after first subsystem moves. |
| `Engine/EntityProcessor.cs` | Script/Render/Scene processors | Shared processor lifecycle hooks (`OnSystemAdd/Remove`, matching) | Keep in `Shared` or `EntityLifecycle` spine until dependency shape stabilizes. |
| `Engine/SceneInstance.cs` | SceneSystem, GraphicsCompositor, render entry points | Bridges scene graph to render visibility/runtime activation | Keep in `SceneLifecycle`; postpone move until ScriptLifecycle precedent is proven. |
| `Engine/Game.cs` | SceneSystem, GameSystem, rendering service graph | Runtime host and service orchestration | Keep in `GameLifecycle`; do not mix with first move. |
| `Engine/Processors/TransformProcessor.cs` | Entity + rendering processors | Crosses entity transform state and renderer updates | Treat as cross-cutting; keep in place until Entity+Rendering dependency seam is documented. |
| `Engine/ScriptComponent.cs` | ScriptSystem + SceneSystem + profiling + audio/sprite services | Script API aggregates multiple services; high coupling hotspot | First move candidate cluster with script files, namespace unchanged. |
| `Rendering/IEntityComponentRenderProcessor.cs` | Multiple rendering processors | Cross-render processor contract used by render subsystems | Keep as `Shared` within RenderingLifecycle boundary. |
| `Engine/Lifecycle/IEntityLifecycleOrchestrator.cs` | EntityManager and lifecycle orchestration entry points | Explicit cross-lifecycle contract | Keep in `Shared` with strict contract-only rule. |

## 8) Move rules (M22b+)

1. Move one lifecycle group at a time.
2. Preserve namespaces initially.
3. Do not mix file moves with nullability or behavior changes.
4. Run focused build/tests after each move slice.
5. Update explicit csproj include/remove entries when paths change.
6. Avoid moving `Shared` until at least two subsystem moves validate dependency shape.
7. Use `Quarantine` only with explicit evidence; do not delete in move passes.
8. Use partial-class splits only when unavoidable.
9. No public API changes during move milestones.
10. Document old path -> new path mapping in each move report.

## 9) First move recommendation

**Recommend M22b target: `ScriptLifecycle`.**

Justification:
- coherent, small-to-medium footprint (`ScriptComponent`, `AsyncScript`, `SyncScript`, `StartupScript`, `ScriptProcessor`, `ScriptSystem`);
- recent audit/cleanup context (M21h) improves confidence;
- dependency shape is known even if coupled (scene/game/profiling services);
- no namespace change required for physical folder move;
- lower risk than rendering/compositor and smaller blast radius than scene lifecycle.

## 10) Validation results

### Command
`dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
- Exit code: `0`
- First meaningful warning/error: `warning CS8767` in `Animations/AnimationChannel.cs`
- Pass/fail: **Pass**
- Output truncated: **No**

### Command
`dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
- Exit code: `0`
- First meaningful warning/error: none (tests passed)
- Pass/fail: **Pass**
- Output truncated: **No**

### Optional command
`dotnet build striv/StriV.Core.slnx -c Debug -v minimal`
- Exit code: `0`
- First meaningful warning/error: none affecting build success
- Pass/fail: **Pass**
- Output truncated: **No**

## Convergence state

**Success** — intended M22a lifecycle-locality mapping capability completed with evidence and no behavior-affecting source changes.
