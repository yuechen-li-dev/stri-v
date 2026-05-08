# M11d Serialization Generator Color3 Proof Validation

## 1) Files changed
- `striv/tests/Stride.Core.Serialization.Generator.Tests/SerializationGeneratorColor3ProofTests.cs`
- `striv/tests/Stride.Core.Serialization.Generator.Tests/Stride.Core.Serialization.Generator.Tests.csproj`
- `docs/stri-v/audits/1000+/1310-m11d-serialization-generator-color3-proof-validation.md`

## 2) Task scope
This task targets a **real Stride type contract shape** (`Stride.Core.Mathematics.Color3`) without migrating production runtime serialization and without disabling AP serialization. No production serialization attributes were removed, and no runtime `Color3` behavior/layout/API was changed.

## 3) Proof strategy chosen
**Option B (Roslyn generator-driver proof)** was chosen.

Why:
- Source generators only see the current compilation; test project references alone cannot source-generate for `Color3` in another assembly.
- A generator-driver test can safely prove contract-shape handling and emitted serializer member order without production registration impact.

What it proves:
- Generator emits `Color3DataSerializer` for a `Color3` contract-shaped real-source subset.
- Generated member serialization order is `R`, `G`, `B`.

What it does not prove:
- Production runtime registration replacement/parity with AP.
- Full real-assembly round-trip for production `Stride.Core.Mathematics.Color3`.

## 4) Color3 contract analysis
From `striv/projects/Stride.Core.Mathematics/Color3.cs`:
- Contract attribute: `[DataContract("Color3")]`
- Type kind: `struct`
- Data members:
  - `[DataMember(0)] public float R;`
  - `[DataMember(1)] public float G;`
  - `[DataMember(2)] public float B;`
- Ignored members: none on these contract members.

Supportability against M11c generator:
- Structs are supported (generator handles `type.IsReferenceType` only for null-init path).
- Fields with `[DataMember(n)]` are supported.
- `float` is supported primitive (`System_Single`).
- Ordering by `DataMember` ctor arg is supported.

Unsupported features discovered relevant to Color3: none blocking this proof.

## 5) Generator changes
No generator code changes were required for Color3 contract-shape proof.

## 6) Generated serializer proof
Test `Generator_EmitsSerializer_ForRealColor3ContractShape_SourceSubset`:
- Reads the real `Color3.cs` and asserts contract markers are present.
- Runs `SerializationGenerator` via Roslyn generator driver against a minimal source subset matching the real contract shape.
- Asserts generated source contains `Color3DataSerializer` and member serialization statements for `R`, `G`, `B` in that exact order.

Compile proof:
- Generator-driver run completed with no generator errors (`DiagnosticSeverity.Error` asserted empty).

Round-trip proof:
- Not included in this option-B proof; runtime registration/round-trip for production `Color3` remains deferred.

## 7) Registration/discovery proof
- Registration/discovery in production assembly was intentionally avoided.
- No production `Color3` serializer registration was added or replaced.
- Remaining parity gap: runtime registration behavior parity for real project integration remains for M11e.

## 8) Tests
Added:
- `Generator_EmitsSerializer_ForRealColor3ContractShape_SourceSubset`
  - Proves generated serializer emission for real Color3 contract shape and member order.

Existing fixture tests remain in project; two currently fail with `NullReferenceException` in fixture serializer runtime path (pre-existing generator/runtime behavior issue outside this M11d scope).

## 9) Generated output inspectability
Inspectability methods used:
- Generator-driver captured source text (`runResult.Results[0].GeneratedSources[...]`).
- Project already has `EmitCompilerGeneratedFiles=true` for inspectable generated files under `obj/.../Generated`.

## 10) Validation results
1. `dotnet test striv/tests/Stride.Core.Serialization.Generator.Tests/Stride.Core.Serialization.Generator.Tests.csproj -v minimal`
   - Exit code: `1`
   - First meaningful error: `System.NullReferenceException` in `FixtureContractDataSerializer.Serialize(...)` during fixture round-trip tests.
   - Result: **fail**
   - Output truncated: yes (tool token truncation).

2. `dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning: large baseline nullability warnings in referenced projects.
   - Result: **pass**
   - Output truncated: yes.

3. `dotnet test striv/tests/Stride.Input.Tests/Stride.Input.Tests.csproj -v minimal`
   - Exit code: `0`
   - First meaningful warning: baseline project warnings during dependency builds.
   - Result: **pass**
   - Output truncated: yes.

4. `./striv/build/striv-check-focused-projects.sh Stride.BepuPhysics Stride.Core.Mathematics Stride.Core.IO Stride.Input`
   - Exit code: `0`
   - First meaningful warning/error: none in summary; all target projects passed with 0 warnings in focused summary.
   - Result: **pass**
   - Output truncated: no.

5. `./striv/build/striv-build-core.sh`
   - Exit code: `0`
   - First meaningful warning: baseline warnings in AP/core builds (no errors).
   - Result: **pass**
   - Output truncated: yes.

## 11) Limitations
- No production project migration to sourcegen.
- No AP serialization disable.
- No replacement of production `Color3` registration.
- No extensions for aliases/profiles/generics/inheritance beyond existing MVP behavior.
- No `EffectBytecode` work.

## 12) Recommended next task
Recommended next task: **M11e Color3 AP/sourcegen parity comparison**, followed by registration parity hardening once parity confidence is established.
