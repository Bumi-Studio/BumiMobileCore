# Bumi Mobile FullSerializer

A packaging of Jacob Weisz's FullSerializer tailored for Bumi Mobile Core. It provides deterministic JSON serialization for complex save graphs, Unity types, and cyclic references so higher-level modules (Save, Security, Monetization, etc.) can persist state without rolling their own format.

## Features

- **Battle-tested serializer** – `fsSerializer`, `fsData`, and the converter pipeline from FullSerializer with all Unity converters included.
- **Unity-aware converters** – Built-in support for `Vector3`, `AnimationCurve`, `Color`, gradients, GUI styles, and other common engine types.
- **Cycles + inheritance** – Handles object graphs with references, polymorphic fields, versioning metadata, and custom processors.
- **Compact JSON** – `fsJsonPrinter.CompressedJson` keeps payloads small, while `PrettyJson` stays available for debugging.
- **Bumi integration** – Distributed as a UPM package with asmdefs named `BumiMobile.FullSerializer` so other Bumi packages consume it directly.

## Requirements

- Unity **2021.3** or newer.
- `com.bumimobile.core` **0.1.1+**.

## Getting Started

1. Add the dependency in your package or asmdef (already done for most Bumi modules).
2. Include the namespace:

```csharp
using FullSerializer;
```

3. Serialize/deserialize your save data:

```csharp
var serializer = new fsSerializer();
fsData data;
serializer.TrySerialize(saveData.GetType(), saveData, out data).AssertSuccessWithoutWarnings();
string json = fsJsonPrinter.CompressedJson(data);

var parsed = fsJsonParser.Parse(json);
object boxed = null;
serializer.TryDeserialize(parsed, saveData.GetType(), ref boxed).AssertSuccessWithoutWarnings();
var restored = (MySave)boxed;
```

Customize behavior through `fsConverter`, `fsObjectProcessor`, or by toggling flags in `fsGlobalConfig`.

## Notes

- The source is kept close to upstream FullSerializer with minimal changes (namespace alignment and asmdef packaging).
- If you need converters for custom Unity types, place them under `FullSerializer.Internal.DirectConverters` inside your project or another Bumi package.

## License

See the repository root `LICENSE` for licensing terms and upstream notices.
