## Stride.FreeImage 5S boundary map (M9b Set in order)

This project is an active desktop runtime bridge around the native FreeImage library.
The goal of this layout pass is to make boundaries explicit before warning cleanup and deeper refactors.

### Folder boundaries

- `Interop/`
  - Raw interop ABI surface (`FreeImageStaticImports`, `Structs`, `Delegates`, `Enumerations`).
  - `FreeImageWrapper` provides managed convenience APIs layered over raw imports.
  - **Invariant:** preserve native signatures and struct layouts unless validated by dedicated ABI tests.
- `Runtime/`
  - Runtime bridge objects (`FreeImageBitmap`, `FreeImageEngine`, stream/memory helpers, plugin repository access).
  - **Invariant:** preserve disposal/ownership behavior for native handles and loaded streams.
- `Metadata/`
  - Metadata model/tag abstractions and model implementations.
  - Broad legacy surface retained for compatibility and later proof-driven reduction.
- `Compatibility/System.Drawing/`
  - Compatibility shims for System.Drawing-related wrapper behavior.
  - Kept intentionally in this phase; removal/replacement is deferred.

### 5S sequencing note

- M9a (Sort): validated active usage and project role.
- M9b (Set in order): reorganizes files and documents boundaries.
- M9c (Shine): warning cleanup and focused safety improvements, explicitly deferred from this pass.
