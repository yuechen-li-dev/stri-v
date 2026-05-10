# 2460 — M23j Processor responsibility split / future replacement map (`Stride.Engine`)

## 1) Files changed
- `docs/stri-v/audits/1000+/2460-m23j-processor-responsibility-split-map.md` (this report only).

## 2) Task scope
This is a **design/audit milestone** that inventories processor/system responsibilities and maps each to a forward replacement path. No implementation, no warnings work, no dependency changes, and no file moves were performed.

## 3) Dominatus reference summary
### What Dominatus separates cleanly
From `ARCHITECTURE.md`, `AUTHORING_GUIDE.md`, and symbol scans across vendored Dominatus + StriV Dominatus adapters/tests:
- **World tick/runtime context** (`AiWorld.Tick`, `AiAgent`, `AiCtx`) is separate from node logic.
- **Behavior authoring** is explicit node/hfsm flow (`AiNode`, `AiStep`, `Goto/Push/Pop/Decide`) and not hidden in engine update processors.
- **State/memory** is explicit typed blackboard (`BbKey<T>`, TTL variants, dirty/revision semantics).
- **Actuation side effects** are explicit commands + typed handlers (`IActuationHandler<T>`, requested/completed transitions), separated from policy.
- **Mailbox/events** are explicit agent communication surfaces, instead of implicit service coupling.

### How this informs processor classification
This split allows classifying existing Stride processors into: query-membership adapters, runtime loop/policy owners, and side-effect executors. Any processor that currently mixes all three is a migration candidate for decomposition: node/policy + actuator + event consumer.

### Why `Stride.Engine` should stay Dominatus-free now
Current Stride.Engine ECS contracts (`EntityComponentChange`, `EntityProcessorMembershipChange`, explicit apply seams) are still being stabilized. Keeping the engine Dominatus-free avoids coupling legacy hot paths to in-flight behavior abstractions while preserving ABI and incremental warning cleanup cadence.

## 4) Current processor framework summary
- **EntityManager role**: owns processor registration, entity add/remove, component change dispatch, dependency revalidation, per-frame `Update`/`Draw` calls, and processor membership application.
- **EntityProcessor role**: owns component matching for its target type(s), associated-data generation/validation, add/remove callbacks, and optional per-frame logic.
- **Matching location**: `EntityManager.CheckEntityComponentWithProcessors(...)` plus `ApplyProcessorMembershipChange(...)` -> `processor.ProcessEntityComponent(...)`.
- **Per-frame update location**: `EntityManager.Update(...)` and `EntityManager.Draw(...)` enumerate enabled processors (and flexible processors).
- **Associated data location**: owned in generic `EntityProcessor<TComponent, TData>` dictionaries (`ComponentDatas`) with `GenerateComponentData` + `IsAssociatedDataValid`.
- **Service injection location**: `EntityManager.OnProcessorAdded(...)` sets `EntityManager` and `Services` on processors; many processors fetch systems in `OnSystemAdd`.

## 5) Processor responsibility table
| Type/file | Current role | Membership/query | Per-frame update | Policy/decision | Side effects/actuation | Runtime state | Services used | Future category | Replacement path |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| `TransformProcessor` | Transform hierarchy maintenance + world matrix propagation | `TransformComponent`, roots/children/scene roots | `Draw` traversal each frame | Order/propagation sequencing | Mutates transforms, entity add/remove via hierarchy changes | root sets, temp component buffers, model-link cache | Dispatcher, SceneInstance/EntityManager | Transform bridge | move side effects behind actuator (later), keep data helper now |
| `ModelNodeLinkProcessor` | Maintains model-node transform links | `ModelNodeLinkComponent` + `TransformComponent` dep | `Draw` pass | Validity/recreate decisions | sets `TransformLink`, hooks hierarchy events | linked component map | EntityManager event hook | Membership compatibility adapter | convert to typed lifecycle event consumer |
| `ModelTransformProcessor` | Registers model post-transform operations | `ModelComponent` + `TransformComponent` dep | mostly add/remove driven | associated-data validity | add/remove transform post operations | transform op associated data | none significant | Data-layer keeper | keep as data-layer helper |
| `CameraProcessor` | camera slot attachment lifecycle for compositor | `CameraComponent` + compositor slot scan | `Draw` each frame | attach/detach/slot conflict policy | mutates camera slots/compositor references, camera updates | compositor ptr, dirty flag | Render tags/compositor | Actuator candidate | move side effects behind camera slot actuator |
| `BackgroundComponentProcessor` | gathers enabled backgrounds list | `BackgroundComponent` | `Draw` rebuild list | enabled filtering | list construction only | `Backgrounds` list | none | Membership compatibility adapter | convert to typed lifecycle event consumer |
| `BackgroundRenderProcessor` | registers render backgrounds | `BackgroundComponent` | draw/render registration phase | active background selection | visibility/render object registration | render objects per component | VisibilityGroup/render services | Render bridge | keep as render bridge for now |
| `ModelRenderProcessor` | model render object creation/updates | `ModelComponent` + transform dep | draw-time updates | render-model visibility/pipeline decisions | registers/unregisters render objects, updates skeleton/render data | render-model map, render meshes | VisibilityGroup, RenderSystem | Render bridge | keep as render bridge for now |
| `SpriteRenderProcessor` | sprite render registration/update | `SpriteComponent` | `Draw` | sprite batching selection | render sprite insertion/removal | sprite info structures | VisibilityGroup/render context | Render bridge | keep as render bridge for now |
| `LightProcessor` | light component to render-light bridge | `LightComponent` | `Draw` | enabled/type update decisions | render light registration and updates | render light map/list | VisibilityGroup/light systems | Render bridge / actuator candidate | keep render bridge now; later actuator boundary |
| `LightProbeProcessor` | light probe render participation | `LightProbeComponent` | draw + add/remove | probe validity/update policy | probe registration/update in render pipeline | probe component state | visibility/rendering services | Render bridge | keep as render bridge for now |
| `InstancingProcessor` | instance data transfer + model instancing map | `InstancingComponent` + transform/model deps | `Draw` | transform usage mode handling | GPU buffer updates, tag map mutation | instancing associated data/map | GraphicsDevice, VisibilityGroup, ModelRenderProcessor | Render bridge | keep as render bridge for now |
| `InstanceProcessor` | parent instancing relation tracking | `InstanceComponent` | update-driven | parent resolution policy | sets instance component linkage | relation state | entity traversal | Legacy behavior processor | defer |
| `AnimationProcessor` | animation evaluation/writeback | `AnimationComponent` | `Draw` heavy loop | blend/time decisions | writes animation results to entities/skeleton | associated data, command lists | services/content timing | Legacy behavior processor | replace with Dominatus node/policy later |
| `LightShaftProcessor` | compose active light-shaft render descriptors | `LightShaftComponent` + light deps | `Update` each frame | filter/eligibility policy | writes active shafts list to tags | active list + associated refs | VisibilityGroup, LightProcessor lookup | Render bridge | keep as render bridge for now |
| `LightShaftBoundingVolumeProcessor` | builds volumes grouped by shaft | `LightShaftBoundingVolumeComponent` | `Update` each frame | dirty rebuild policy | regenerates grouped volumes | dictionary + dirty bit | none major | Membership compatibility adapter | convert to typed lifecycle consumer |
| `ScriptProcessor` | attach/detach script components to script runtime | `ScriptComponent` | add/remove centered | none/minimal | calls `ScriptSystem.Add/Remove` | script system ref | `ScriptSystem` service | Script/Dominatus replacement candidate | replace with Dominatus node/policy |
| `ScriptSystem` | global script scheduler/runtime | script lists (not entity query) | `Update` every frame | execution order/error policy | runs scripts, schedules tasks, mutates script state | scheduler, lists, priority queues | Game services/content/time | Script/Dominatus replacement candidate | replace with Dominatus node/policy + action actuator |
| `GameSystem` | base game-level service host | n/a | framework | n/a | n/a | game ref passthrough | IServiceRegistry | Data-layer keeper | keep as data-layer helper |
| `DebugTextSystem` | on-screen debug text aggregation | n/a + producers | `Update` + `Draw` | text ordering/expiry policy | writes debug overlay batches | text stores | graphics/font/input services | Diagnostics/debug system | keep debug bridge; defer redesign |
| `GameProfilingSystem` | profiling collection/render display | n/a | `Draw`/timed updates | profiler sampling/format policy | mutates profiler buffers and UI | profiling history/state | profiling+graphics services | Diagnostics/debug system | keep debug bridge; defer |
| `SpriteAnimationSystem` | sprite animation stepping | sprite component sets | `Draw` | animation time decisions | updates sprite animation states | component refs/time | game timing | Legacy behavior processor | replace with Dominatus node/policy later |
| `SceneSystem` (processor-like) | scene update+draw orchestration incl. render processors | scene/visibility groups | `Update` + `Draw` | renderer selection/orchestration | drives scene instance/render passes | scene instances, render context | game/render services | Needs design | defer |
| Quarantine audio processors/systems | legacy audio lifecycle and updates | audio components | frame + event | listener/emitter policy | audio engine side effects | audio runtime objects | AudioSystem/services | Delete/quarantine candidate | delete/quarantine |

## 6) Family analysis
- **Transform/data hierarchy**: `TransformProcessor`, `ModelTransformProcessor`, `ModelNodeLinkProcessor` are split between data maintenance and transform-side effects. `ModelTransformProcessor` is closest to data-layer keeper; `TransformProcessor` is main future transform-actuator bridge.
- **Rendering**: `ModelRenderProcessor`, `SpriteRenderProcessor`, `BackgroundRenderProcessor`, `InstancingProcessor` are required compatibility bridges to current render backend; keep temporary.
- **Camera**: `CameraProcessor` mixes slot-policy + side effects; strong actuator-boundary candidate.
- **Lights/probes**: `LightProcessor`, `LightProbeProcessor`, `LightShaft*` are render-bridge and should stay until render actuator boundaries mature.
- **Script/behavior**: `ScriptSystem`/`ScriptProcessor` and behavior-heavy loops (including `AnimationProcessor`/`SpriteAnimationSystem`) align with future Dominatus policy-node migration.
- **Diagnostics**: `DebugTextSystem`, `GameProfilingSystem` remain diagnostics bridges; not primary migration path.
- **Legacy/quarantine**: quarantine audio processors should not be beautified; only maintain enough for containment/removal.

## 7) Target architecture
### Data ECS layer
Owns `Entity`, `EntityComponent`, component storage/collections, `TransformComponent`, scene membership. Does **not** own behavior policy, AI/script loops, render submission policy, or open-ended service injection.

### Lifecycle event layer
Owns `EntityComponentChange`, `EntityProcessorMembershipChange`, and future typed lifecycle payloads for scene/transform membership changes. Role: explicit attach/detach/change/membership feed for compatibility processors now and Dominatus adapters later.

### Compatibility processor layer
Temporary adapters over existing processor callbacks/events. Side effects remain in legacy processors until actuator seams exist. No new processor abstraction expansion.

### Dominatus behavior layer (future)
`AiWorld.Tick` replaces behavior loops; HFSM nodes hold policy/decision flow; blackboard replaces ad-hoc runtime/service lookup patterns where practical.

### Actuator boundary (future)
Primary boundaries: transform lifecycle actuator, render registration actuator, camera slot actuator, light/probe actuator, script/action actuator, optional diagnostics display actuator.

## 8) Migration order recommendation
1. **Finish data-layer contracts** (entity/component/transform/scene explicit contracts).
2. **Stabilize lifecycle event contracts** (component changes, processor membership, scene membership, transform parent change).
3. **Isolate side effects behind adapters** (transform, membership, render registration, camera slots, light/probe registration).
4. **Replace script/behavior loops** (`ScriptSystem`/`SyncScript`/`AsyncScript` path into Dominatus/OptFlow nodes).
5. **Keep render bridges temporarily** (`ModelRenderProcessor`, `SpriteRenderProcessor`, `LightProcessor`, etc.) until actuator seams stabilize.
6. **Delete/quarantine legacy** processors/subsystems once replacement path is proven.

## 9) Warning cleanup implications
| Warning family | Current files | What it likely means | Best cleanup strategy |
| --- | --- | --- | --- |
| EntityManager CS8618/CS8604/CS8625 | `EntityManager.cs` | nullable lifecycle/state injection seams not explicit enough | data-layer contract cleanup + constructor/event nullability hardening |
| Processor associated-data warnings | multiple `EntityProcessor<T,TData>` implementations | associated data validity vs nullable refs is implicit | event/membership contract cleanup with stricter typed payloads |
| CameraProcessor slot warnings | `CameraProcessor.cs` | slot attach/detach nullable and conflict cases | actuator-boundary cleanup (camera slot actuator) |
| Render processor warnings | render processor files | visibility/render object lifecycle states are multi-phase | actuator-boundary cleanup, keep bridges temporary |
| ScriptSystem warnings | `ScriptSystem.cs`, `ScriptProcessor.cs` | scheduler/script refs nullability and lifecycle races | true Dominatus migration later + interim contract hardening |
| Diagnostics warnings | `DebugTextSystem.cs`, `GameProfilingSystem.cs` | service lookup and optional render resources | defer, keep diagnostics isolated |
| UpdateEngine/related warnings (if surfaced) | updater/reflection and update-calling paths | legacy update pipeline assumptions | classify as legacy compatibility; defer unless blocking contracts |

## 10) Next implementation recommendation
**Best next slice: EntityManager event/constructor cleanup.**
Rationale: it is the central seam for membership and component change application and will reduce nullable-warning noise while improving determinism before actuator extraction.

## 11) Validation results
1. Command: `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental`
   - Exit code: 0
   - First meaningful warning/error: multiple existing nullable warnings in Stride.Engine processors/systems (baseline continues)
   - Pass/fail: Pass
   - Output truncated: No

2. Command: `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal`
   - Exit code: 0
   - First meaningful warning/error: no blocking errors; tests complete successfully
   - Pass/fail: Pass
   - Output truncated: No
