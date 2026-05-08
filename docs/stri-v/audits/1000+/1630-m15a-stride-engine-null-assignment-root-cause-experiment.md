# M15a Stride.Engine null-assignment root-cause experiment

## 1) Files changed
- `striv/projects/Stride.Engine/Engine/Entity.cs`
- `striv/projects/Stride.Engine/Engine/Design/EntityCloner.cs`

## 2) Task scope
Focused root-cause experiment on null-assignment patterns in `Stride.Engine` only. Not a full Engine Shine pass. No broad refactor or suppression.

## 3) Hypothesis
Legacy Stride code uses `= null` for multiple semantics (placeholder, lifecycle reset, semantic absence, etc.). This likely amplifies NRT warning cascades. This experiment checks whether classifying/fixing obvious cases reduces warnings.

## 4) Before warnings
- Focused warning lines: **1008**
- Top codes:
  - CS8618: 348
  - CS8625: 164
  - CS8600: 90
  - CS8604: 84
  - CS8602: 76
- Top buckets (sample):
  - `Engine/ScriptComponent.cs CS8618` (28)
  - `Rendering/Compositing/ForwardRenderer.cs CS8618` (24)
  - `Engine/SceneInstance.cs CS8622` (22)

## 5) Null assignment classification summary
| Category | Count/examples | Fix strategy | Fixed/deferred |
| --- | ---: | --- | --- |
| Public semantic nullable | Entity.Scene allows null to detach/remove | Mark contract nullable (`Scene?`) | Fixed |
| Serialization placeholder | `T result = null;` before `SerializeExtended(ref result, ...)` | `T? result = default` + post-deserialize guard | Fixed |
| Lifecycle destroy/reset | Clone context fields reset to null in cleanup | Mark backing fields nullable | Fixed |
| Behavior-sensitive lifecycle | script/system/graphics cleanup nulling | defer without tests | Deferred |
| Broad constructor/required-field CS8618 | many processors/services | out of focused null-assignment scope | Deferred |

## 6) Fixes applied
- `Entity.cs`: `SceneValue` and `Scene` changed to nullable to reflect existing documented behavior of setting scene to null; ctor optional name annotated nullable.
- `EntityCloner.cs`: serializer selectors/context references made nullable where cleanup assigns null; placeholder deserialize variable changed from `T result = null` to `T? result = default` with explicit guard before return.

Behavior impact: intended to be contract-accurate or mechanical only. No runtime-path algorithm changes.

## 7) Tests
No new tests added. Changes were limited to obvious mechanical/contract nullability updates and cleanup-field annotations.

## 8) After warnings
- Focused warning lines: **980**
- Top codes:
  - CS8618: 340
  - CS8625: 144
  - CS8600: 86
  - CS8604: 84
  - CS8602: 82
- Delta: **-28** focused warning lines.
- Result: hypothesis appears **promising**; small targeted null-assignment fixes produced measurable warning reduction.

## 9) Deferred patterns
Deferred due to behavior sensitivity or broader scope:
- serialization-heavy clones beyond straightforward placeholder site
- graphics/resource lifecycle (renderers, textures, devices)
- entity/script/system lifecycle resets
- broad constructor required-member/initialization graph

## 10) Validation results
- `dotnet build striv/projects/Stride.Engine/Stride.Engine.csproj -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, warnings present, output truncated in terminal capture: yes.
- `./striv/build/striv-check-focused-project.sh Stride.Engine` → exit 4 (focused warning gate fail), first meaningful message: `Focused warning gate failed for Stride.Engine`.
- `dotnet build striv/StriV.Core.slnx -c Debug -p:StriVWarningFocusProject=Stride.Engine --no-incremental` → exit 0, warnings present, terminal output truncated: yes.

(Part 7 full matrix was not fully executed in this pass.)

## 11) Recommendation
Continue a scoped Engine null-assignment root-cause pass (same methodology), prioritizing public semantic nullable contracts and mechanical placeholder/lifecycle field annotations with low behavior risk, then reassess warning collapse trend before broader Engine Sort.
