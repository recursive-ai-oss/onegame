[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# ISteamCallbackData&lt;TSelf&gt; interface

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface ISteamCallbackData<TSelf> where TSelf : struct, ISteamCallbackData<TSelf>
```

A callback or call-result struct Steam can deliver, decoded from its raw bytes.

**Type parameters**

- `TSelf`: The struct itself.

## Properties

| Member | Description |
| --- | --- |
| [`CallbackId`](#m-callbackid) | The callback's id, which tags it in the dispatch loop. |
| [`Fields`](#m-fields) | The fields in declaration order, used to compute the native layout. |

### <a id="m-callbackid"></a>CallbackId

```csharp
static abstract int CallbackId { get; }
```

The callback's id, which tags it in the dispatch loop.

**Type:** `int`

### <a id="m-fields"></a>Fields

```csharp
static abstract ReadOnlySpan<SteamField> Fields { get; }
```

The fields in declaration order, used to compute the native layout.

**Type:** [`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)&lt;[`SteamField`](SteamField.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`TryRead(ReadOnlySpan<byte>, SteamPacking, out TSelf)`](#m-tryread-readonlyspan-byte-steampacking-tself) | Decodes the value from `data`, laid out for `packing`. |

### <a id="m-tryread-readonlyspan-byte-steampacking-tself"></a>TryRead(ReadOnlySpan&lt;byte&gt;, SteamPacking, out TSelf)

```csharp
static abstract bool TryRead(ReadOnlySpan<byte> data, SteamPacking packing, out TSelf value);
```

Decodes the value from `data`, laid out for `packing`. Returns false when the data is too short, so a short or garbled message is ignored rather than read past its end.

**Parameters**

- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)
- `packing` ([`SteamPacking`](SteamPacking.md))
- `value` (`TSelf`)

**Returns:** `bool`
