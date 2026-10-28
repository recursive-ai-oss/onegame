[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# Entropy static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class Entropy
```

Unpredictable bytes for connection ids, handshake secrets and keys. Never use it in simulation code: sim randomness comes from named streams so replays match.

The default source avoids the runtime's crypto provider, which needs a system library that Android apps lack: Windows asks the OS through the runtime (its provider is built in), every other system reads `/dev/urandom` (the kernel's generator on Linux and Android, `arc4random` on Apple systems). A platform without that device installs its own source with [`Entropy.Use()`](Entropy.md#m-use-ientropysource) before the first network connection.

## Methods

| Member | Description |
| --- | --- |
| [`Fill(Span<byte>)`](#m-fill-span-byte) | Fills `buffer` with unpredictable bytes. |
| [`Use(IEntropySource)`](#m-use-ientropysource) | Replaces the source for the whole process. |

### <a id="m-fill-span-byte"></a>Fill(Span&lt;byte&gt;)

```csharp
public static void Fill(Span<byte> buffer);
```

Fills `buffer` with unpredictable bytes.

**Parameters**

- `buffer` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)

**Exceptions**

- [`PlatformNotSupportedException`](https://learn.microsoft.com/dotnet/api/system.platformnotsupportedexception): No source is available on this platform and none was installed with [`Entropy.Use()`](Entropy.md#m-use-ientropysource).

### <a id="m-use-ientropysource"></a>Use(IEntropySource)

```csharp
public static void Use(IEntropySource source);
```

Replaces the source for the whole process.

**Parameters**

- `source` ([`IEntropySource`](IEntropySource.md))
