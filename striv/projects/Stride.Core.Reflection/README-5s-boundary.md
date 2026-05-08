# Stride.Core.Reflection boundary (M14d)

## Intended long-term role
`Stride.Core.Reflection` is a runtime metadata/member-shape layer that supports serialization and assembly-processing compatibility paths.

## Keep zones (load-bearing)
- `TypeDescriptorFactory`
- `TypeDescriptors/*` core descriptor behavior
- `MemberDescriptors/*`
- `AttributeRegistry`
- descriptor/member metadata logic consumed by `Stride.Core.Serialization`

## Conservative Sort zones
- `TypeExtensions.cs`: thin-wrapper helpers should be replaced incrementally at callsites when semantics are mechanical and build-proven.
- `OldCollectionDescriptor`: obsolete compatibility fallback. It is retained only for legacy collection shapes that are not selected by `ListDescriptor`, `SetDescriptor`, `DictionaryDescriptor`, or `ArrayDescriptor`.

## OldCollectionDescriptor compatibility rule
- `OldCollectionDescriptor` is not future architecture.
- No new Stri-V code should depend on this fallback path.
- Keep namespace/type identity stable while quarantined.
- Remove only after descriptor-selection tests prove no active consumers.

## Deferred coupling
- `MemberPath` and related AP/sourcegen-sensitive behavior are deferred due to compatibility/serializability pressure.

## Rule for new Stri-V code
Prefer direct BCL reflection/type APIs unless descriptor-pipeline behavior is explicitly required.
