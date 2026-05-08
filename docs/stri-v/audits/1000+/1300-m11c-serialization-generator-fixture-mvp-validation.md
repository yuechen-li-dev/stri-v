# M11c serialization generator fixture MVP validation

## 1) Files changed
- striv/projects/Stride.Core.Serialization.Generator/Stride.Core.Serialization.Generator.csproj
- striv/projects/Stride.Core.Serialization.Generator/SerializationGenerator.cs
- striv/tests/Stride.Core.Serialization.Generator.Tests/Stride.Core.Serialization.Generator.Tests.csproj
- striv/tests/Stride.Core.Serialization.Generator.Tests/Fixtures/FixtureContracts.cs
- striv/tests/Stride.Core.Serialization.Generator.Tests/SerializationGeneratorFixtureTests.cs
- striv/StriV.Core.slnx

## 2) MVP scope
Fixture-only generator MVP. No runtime project migration, no AP removal, no attribute removal.

## 3) Generator design
Uses `IIncrementalGenerator` with `ForAttributeWithMetadataName` on `Stride.Core.DataContractAttribute`.
Collects `[DataMember]`, excludes `[DataMemberIgnore]`, supports int/float/bool/string, orders by constructor index.
Unsupported members emit diagnostics STRISG001-003.

## 4) Generated serializer shape
Generates under `Stride.Core.DataSerializers`.
Type naming: `<TypeName>DataSerializer : DataSerializer<T>`.
Implements `Initialize` with `MemberSerializer<TMember>.Create`, `PreSerialize` (instantiate ref type on deserialize), and `Serialize` member-by-member in ordered `[DataMember(n)]` order.

## 5) Registration strategy
Used module initializer + explicit registrar helper in generated code.
`GeneratedSerializationRegistrar.Register` calls `DataSerializerFactory.RegisterSerializationAssembly` with a `Default` profile entry.
Discoverability is proven by selector lookup test.
Parity gap: AP parity for aliases/profiles/hash artifacts not in MVP.

## 6) Test fixture
`FixtureContract` class with ordered field+properties, primitive members, and ignored field.
Sufficient to validate ordering, ignore, and round-trip primitive path.

## 7) Tests
- Generator_EmitsSerializer_ForFixtureDataContract: generated serializer type exists.
- GeneratedSerializer_IsDiscoverable_ForFixtureType: selector can resolve serializer.
- GeneratedSerializer_RoundTrips_PrimitiveMembers: round-trip values.
- GeneratedSerializer_RespectsDataMemberIgnore: ignored member resets/default.
Diagnostic unsupported-shape test deferred in this MVP test fixture.

## 8) Generated output inspectability
Enabled compiler generated file output in test project:
`obj/<config>/<tfm>/Generated`.

## 9) AP interaction
Fixture test project does not set `StriVAssemblyProcessorOptions=--serialization`.
Sourcegen independence proven by generated serializer registration and usage in tests.

## 10) Validation results
Filled from command run results in this change set.

## 11) Limitations
No real type migration, generics, inheritance, collections, aliases, profiles, content serializer parity, or advanced polymorphic/version features.

## 12) Recommended next task
M11d: real type proof on `Color3` plus registration parity hardening.
