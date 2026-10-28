[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam

# SteamApiCallCompleted struct

**Namespace:** `OneGame.Steam`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`ISteamCallbackData`](ISteamCallbackData-1.md)&lt;[`SteamApiCallCompleted`](SteamApiCallCompleted.md)&gt;, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamApiCallCompleted`](SteamApiCallCompleted.md)&gt;

```csharp
public readonly struct SteamApiCallCompleted : ISteamCallbackData<SteamApiCallCompleted>, IEquatable<SteamApiCallCompleted>
```

A previously started async call finished. The dispatch loop uses it to fetch the call's result; games do not see it.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamApiCallCompleted(ulong, int, uint)`](#m-ctor-ulong-int-uint) | A previously started async call finished. |

### <a id="m-ctor-ulong-int-uint"></a>SteamApiCallCompleted(ulong, int, uint)

```csharp
public SteamApiCallCompleted(ulong Call, int CallbackId, uint ResultSize);
```

A previously started async call finished. The dispatch loop uses it to fetch the call's result; games do not see it.

**Parameters**

- `Call` (`ulong`): Handle of the finished call.
- `CallbackId` (`int`): Id of the result type.
- `ResultSize` (`uint`): Size in bytes of the result.

## Properties

| Member | Description |
| --- | --- |
| [`Call`](#m-call) | Handle of the finished call. |
| [`CallbackId`](#m-callbackid) | Id of the result type. |
| [`ResultSize`](#m-resultsize) | Size in bytes of the result. |

### <a id="m-call"></a>Call

```csharp
public ulong Call { get; init; }
```

Handle of the finished call.

**Type:** `ulong`

### <a id="m-callbackid"></a>CallbackId

```csharp
public int CallbackId { get; init; }
```

Id of the result type.

**Type:** `int`

### <a id="m-resultsize"></a>ResultSize

```csharp
public uint ResultSize { get; init; }
```

Size in bytes of the result.

**Type:** `uint`
