# Stride.Core.Reflection boundary (M14b)

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
- `OldCollectionDescriptor`: legacy compatibility fallback for collection handling; do not remove without consumer-proof migration.

## Deferred coupling
- `MemberPath` and related AP/sourcegen-sensitive behavior are deferred due to compatibility/serializability pressure.

## Rule for new Stri-V code
Prefer direct BCL reflection/type APIs unless descriptor-pipeline behavior is explicitly required.
