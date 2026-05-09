# 1770 - M16l engine lifecycle composition proof

## 1) Files changed
- `striv/tests/StriV.Engine.Dominatus.Tests/Integration/EngineLifecycleCompositionTests.cs` (new).
- Production files changed: none.

## 2) Task scope
This task adds one opt-in integration proof that composes transform + scene + processor transitions through the production Stride lifecycle actuators. No runtime rewiring was performed, and no default Stride.Engine update flow was modified.

## 3) Composition design
Attempted sequence goal was transform attach -> scene attach -> processor add -> processor entity add.

Observed compatibility rule (from existing behavior and enforced in this test): scene attachment is performed before non-null transform parenting to avoid illegal parented-entity scene assignment.

Implemented flow in one lifecycle test:
1. Scene attach parent entity.
2. Scene attach child entity.
3. Transform attach child to parent.
4. Processor system add.
5. Processor entity add.

This preserves a single end-to-end lifecycle composition while respecting current engine ordering constraints.

## 4) Tests
### `EngineLifecycleComposition_TransformSceneProcessorTransitions_ComposeThroughProductionAdapters`
Transitions composed:
- `SceneLifecycleTransition.AttachEntityAsync`
- `TransformLifecycleTransition.AttachParentAsync`
- `ProcessorLifecycleTransition.AddProcessorToSystemAsync`
- `ProcessorLifecycleTransition.AddEntityToProcessorAsync`

Production adapters used:
- `StrideSceneLifecycleActuator`
- `StrideTransformLifecycleActuator`
- `StrideProcessorLifecycleActuator`

Assertions:
- Completed events return expected entity/scene/parent/processor/manager references.
- Stride state updates: scene membership, transform parent/children, processor manager binding.
- Processor callback behavior: `OnEntityComponentAdding` invoked once with child entity.

## 5) Behavior compatibility
- No engine runtime behavior changed.
- No direct Dominatus dependency added to `Stride.Engine`.
- Composition remains test-only and opt-in through adapter/transition usage.

## 6) Lessons
- The current bridge stack composes cleanly for a real lifecycle slice when ordering constraints are honored.
- Scene/transform ordering is a real lifecycle policy boundary and should be explicit in later migration doctrine.
- The `EntityManager` processor entity seam is sufficient for integration-grade add-flow proofing through production adapters.

## 7) Validation results
| Command | Exit | First meaningful warning/error | Pass/Fail | Output truncated |
|---|---:|---|---|---|
| `dotnet build striv/projects/StriV.Engine.Dominatus/StriV.Engine.Dominatus.csproj -c Debug -v minimal` | 0 | Existing legacy warning set (e.g., CS1030 PERF warnings in `Stride.Core`) | Pass | Yes |
| `dotnet build striv/projects/StriV.Engine.Dominatus.Adapters/StriV.Engine.Dominatus.Adapters.csproj -c Debug -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental 2>&1 | tee /tmp/striv-m16l-engine-focused.log` | 0 | Existing engine warnings (e.g., CS8765 in `CompressedTimeSpan`) | Pass | Yes |
| `dotnet build striv/StriV.Core.slnx -c Debug -v minimal` | 0 | Existing warning set in assembly processor/reflection tests | Pass | Yes |
| `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input Stride.Games Stride.Core.Reflection` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Engine.Tests/Stride.Engine.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.Engine.Dominatus.Tests/StriV.Engine.Dominatus.Tests.csproj -v minimal` | 0 | None | Pass | No |
| `dotnet test striv/tests/Stride.Core.Reflection.Tests/Stride.Core.Reflection.Tests.csproj -v minimal` | 0 | Existing obsolete warnings in test fixture (`OldCollectionDescriptor`) | Pass | Yes |
| `dotnet test striv/tests/Stride.Games.Tests/Stride.Games.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetTool.Tests/StriV.AssetTool.Tests.csproj -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.AssetPipeline.Tests/StriV.AssetPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | Yes |
| `dotnet test striv/tests/StriV.ShaderPipeline.Tests/StriV.ShaderPipeline.Tests.csproj --no-build -v minimal` | 0 | None | Pass | Yes |
| `./striv/build/striv-build-core.sh` | 0 | None | Pass | Yes |

## 8) Recommended next task
**M16m strangler doctrine / migration map**.

Reason: this proof confirms composed lifecycle viability through production adapters and identifies scene/transform ordering as policy. The highest leverage next step is a documented migration map that formalizes lifecycle ordering doctrine and rollout boundaries before runtime opt-in prototypes.
