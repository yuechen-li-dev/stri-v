# 570 — FreeImage nullability pilot validation

## 1) Files changed
- `sources/tools/Stride.FreeImage/Classes/FreeImageBitmap.cs`
- `sources/tools/Stride.FreeImage/Classes/ImageMetadata.cs`
- `sources/tools/Stride.FreeImage/Classes/MemoryArray.cs`
- `sources/tools/Stride.FreeImage/Classes/MetadataTag.cs`
- `sources/tools/Stride.FreeImage/Structs/BITMAPINFO.cs`
- `sources/tools/Stride.FreeImage/Structs/BITMAPINFOHEADER.cs`
- `sources/tools/Stride.FreeImage/Structs/FI16RGB555.cs`
- `sources/tools/Stride.FreeImage/Structs/FI16RGB565.cs`
- `sources/tools/Stride.FreeImage/Structs/FIBITMAP.cs`
- `sources/tools/Stride.FreeImage/Structs/FICOMPLEX.cs`
- `sources/tools/Stride.FreeImage/Structs/FIMEMORY.cs`
- `sources/tools/Stride.FreeImage/Structs/FIMETADATA.cs`
- `sources/tools/Stride.FreeImage/Structs/FIMULTIBITMAP.cs`
- `sources/tools/Stride.FreeImage/Structs/FIRGB16.cs`
- `sources/tools/Stride.FreeImage/Structs/FIRGBA16.cs`
- `sources/tools/Stride.FreeImage/Structs/FIRGBAF.cs`
- `sources/tools/Stride.FreeImage/Structs/FIRGBF.cs`
- `sources/tools/Stride.FreeImage/Structs/FIRational.cs`
- `sources/tools/Stride.FreeImage/Structs/FITAG.cs`
- `sources/tools/Stride.FreeImage/Structs/FIURational.cs`
- `sources/tools/Stride.FreeImage/Structs/RGBQUAD.cs`
- `sources/tools/Stride.FreeImage/Structs/RGBTRIPLE.cs`
- `sources/tools/Stride.FreeImage/Structs/fi_handle.cs`
- `docs/stri-v/audits/570-freeimage-nullability-pilot-validation.md`

## 2) Pilot scope
- `Stride.FreeImage` was selected because audit `560-clean-graph-nullable-strategy.md` recommended a bounded low-risk nullable pilot outside runtime-heavy Engine/Rendering/Graphics.
- Targeted warning codes: `CS8765`, `CS8767`, `CS8769`.
- This is lower risk because edits are constrained to signature nullability alignment (override/interface contract matching), mostly in interop/value wrapper types, with no architecture/runtime-flow changes.

## 3) Baseline
Commands:
```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-freeimage-before.log
grep -E "warning CS(8765|8767|8769)" /tmp/striv-nullable-freeimage-before.log | grep "Stride.FreeImage" | tee /tmp/striv-freeimage-876x-before.log
wc -l /tmp/striv-freeimage-876x-before.log
```

- Baseline count: **162** FreeImage warnings (`CS8765/CS8767/CS8769`).
- Representative examples:
  - `FreeImageBitmap.Equals(object obj)` vs `object?` override contract.
  - `CompareTo(object obj)` vs `IComparable.CompareTo(object? obj)`.
  - `IConvertible.*(IFormatProvider provider)` vs `IFormatProvider? provider`.

## 4) Fixes applied
### Group A: `Equals(object)` override alignment (`CS8765`)
- Old pattern: `public override bool Equals(object obj)`
- New pattern: `public override bool Equals(object? obj)`
- Why: matches `object.Equals(object? obj)` override contract.
- Behavior change: none (existing logic already handled non-matching inputs).

### Group B: non-generic `IComparable` alignment (`CS8767`)
- Old pattern: `public int CompareTo(object obj)`
- New pattern: `public int CompareTo(object? obj)`
- Why: matches `IComparable.CompareTo(object? obj)` contract.
- Behavior change: none.

### Group C: `IFormattable` alignment (`CS8767`)
- Old pattern: `public string ToString(string format, IFormatProvider formatProvider)`
- New pattern: `public string ToString(string? format, IFormatProvider? formatProvider)`
- Why: matches `IFormattable.ToString(string? format, IFormatProvider? formatProvider)`.
- Behavior change: none.

### Group D: `IConvertible` explicit implementation alignment (`CS8769`)
- Old pattern: `... (IFormatProvider provider)`
- New pattern: `... (IFormatProvider? provider)`
- Why: BCL `IConvertible` members permit nullable `IFormatProvider?`.
- Behavior change: none.

### Group E: generic reference-type interface alignment (`CS8767`)
- In class types (`MetadataTag`, `ImageMetadata`, `MemoryArray<T>`):
  - `CompareTo(T other)` -> `CompareTo(T? other)` where warning indicated nullable contract.
  - `Equals(T other)` -> `Equals(T? other)` where warning indicated nullable contract.
- Why: match nullable generic interface expectations.
- Behavior change: none.

### Note on constrained structs
- Attempting nullable generic signatures for struct interfaces in `FIRational`/`FIURational` caused `CS0535` interface implementation errors, so those generic struct methods were reverted to non-nullable value signatures while keeping other nullable contract alignments.

## 5) After counts
Commands:
```bash
./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-freeimage-after.log
grep -E "warning CS(8765|8767|8769)" /tmp/striv-nullable-freeimage-after.log | grep "Stride.FreeImage" | tee /tmp/striv-freeimage-876x-after.log
wc -l /tmp/striv-freeimage-876x-after.log
```

- After count: **0**
- Delta: **-162**
- Remaining `CS8765/CS8767/CS8769` in FreeImage: none.

## 6) Validation results
1. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-freeimage-before.log`
   - Exit code: 0
   - First meaningful warning/error: first warnings were nullable warnings in non-FreeImage projects (e.g., `Stride.Core.AssemblyProcessor`).
   - Pass/Fail: Pass
   - Output truncated: yes (tool output display truncated; full log retained in `/tmp`).

2. `grep -E "warning CS(8765|8767|8769)" /tmp/striv-nullable-freeimage-before.log | grep "Stride.FreeImage" | tee /tmp/striv-freeimage-876x-before.log`
   - Exit code: 0
   - First meaningful warning/error: `FreeImageBitmap.cs ... CS8765`
   - Pass/Fail: Pass
   - Output truncated: yes (display truncated; file written in `/tmp`).

3. `wc -l /tmp/striv-freeimage-876x-before.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/Fail: Pass
   - Output truncated: no

4. `./striv/build/striv-build-core.sh 2>&1 | tee /tmp/striv-nullable-freeimage-after.log`
   - Exit code: 0
   - First meaningful warning/error: first warnings were non-target nullable warnings in other projects; no FreeImage `876x` remained.
   - Pass/Fail: Pass
   - Output truncated: yes (display truncated; full log retained in `/tmp`).

5. `grep -E "warning CS(8765|8767|8769)" /tmp/striv-nullable-freeimage-after.log | grep "Stride.FreeImage" | tee /tmp/striv-freeimage-876x-after.log`
   - Exit code: 0
   - First meaningful warning/error: none
   - Pass/Fail: Pass
   - Output truncated: no

6. `wc -l /tmp/striv-freeimage-876x-after.log`
   - Exit code: 0
   - First meaningful warning/error: n/a
   - Pass/Fail: Pass
   - Output truncated: no

## 7) Tests
Command:
```bash
dotnet test striv/tests/StriV.CleanGraph.Tests/StriV.CleanGraph.Tests.csproj --no-build -v minimal
```

- Pass/Fail: Pass (4 passed, 0 failed).
- What this proves: clean-graph test project still passes after FreeImage signature alignment changes.
- What this does not prove: it does not exhaustively validate all FreeImage runtime behaviors or image format edge-cases.

## 8) Stop/defer notes
- No semantic/risky FreeImage warning fixes were attempted.
- Non-target nullable warnings (e.g., CS860x/CS8618) remain in FreeImage and were intentionally deferred by scope.
- Work stopped at mechanical signature alignment only.

## 9) Worktree status
Command:
```bash
git status --short
```

Result: modified FreeImage files listed in section (1), plus this report.

## 10) Recommended next task
- **Another FreeImage nullable pilot**: target a similarly bounded subset (e.g., specific `CS8603` return-nullability mismatches in a small, isolated area) with the same evidence-first workflow.
