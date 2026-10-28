[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# StateHash static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class StateHash
```

A deterministic 64-bit hash of bytes (FNV-1a with a final avalanche), used for drift detection. Not for security.

## Methods

| Member | Description |
| --- | --- |
| [`Combine(ulong, ulong)`](#m-combine-ulong-ulong) | Folds `value` into `hash`; the result depends on the order of folding. |
| [`Fnv32(string)`](#m-fnv32-string) | The 32-bit FNV-1a hash of a string's UTF-8 bytes, used for stable type and command ids. |
| [`Of(ReadOnlySpan<byte>)`](#m-of-readonlyspan-byte) | Hashes `bytes`. |
| [`Of(string)`](#m-of-string) | Hashes a string by its UTF-8 bytes. |

### <a id="m-combine-ulong-ulong"></a>Combine(ulong, ulong)

```csharp
public static ulong Combine(ulong hash, ulong value);
```

Folds `value` into `hash`; the result depends on the order of folding.

**Parameters**

- `hash` (`ulong`)
- `value` (`ulong`)

**Returns:** `ulong`

### <a id="m-fnv32-string"></a>Fnv32(string)

```csharp
public static uint Fnv32(string text);
```

The 32-bit FNV-1a hash of a string's UTF-8 bytes, used for stable type and command ids.

**Parameters**

- `text` (`string`)

**Returns:** `uint`

### <a id="m-of-readonlyspan-byte"></a>Of(ReadOnlySpan&lt;byte&gt;)

```csharp
public static ulong Of(ReadOnlySpan<byte> bytes);
```

Hashes `bytes`.

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `ulong`

### <a id="m-of-string"></a>Of(string)

```csharp
public static ulong Of(string text);
```

Hashes a string by its UTF-8 bytes.

**Parameters**

- `text` (`string`)

**Returns:** `ulong`
