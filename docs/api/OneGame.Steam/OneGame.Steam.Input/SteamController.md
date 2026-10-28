[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamController record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamController`](SteamController.md)&gt;

```csharp
public sealed record SteamController : IEquatable<SteamController>
```

A controller Steam Input reports. [`SteamController.Index`](SteamController.md#m-index) is stable while the controller stays connected and is what the harness and scenarios name it by.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamController(int, ulong, int)`](#m-ctor-int-ulong-int) | A controller Steam Input reports. |

### <a id="m-ctor-int-ulong-int"></a>SteamController(int, ulong, int)

```csharp
public SteamController(int Index, ulong Handle, int InputType);
```

A controller Steam Input reports. [`SteamController.Index`](SteamController.md#m-index) is stable while the controller stays connected and is what the harness and scenarios name it by.

**Parameters**

- `Index` (`int`): 1-based number, the lowest free one when the controller connected.
- `Handle` (`ulong`): Steam's handle.
- `InputType` (`int`): Steam's input type number (3 is an Xbox One pad, 13 a PS5 pad, 14 the Steam Deck).

## Properties

| Member | Description |
| --- | --- |
| [`Handle`](#m-handle) | Steam's handle. |
| [`Index`](#m-index) | 1-based number, the lowest free one when the controller connected. |
| [`InputType`](#m-inputtype) | Steam's input type number (3 is an Xbox One pad, 13 a PS5 pad, 14 the Steam Deck). |
| [`TypeName`](#m-typename) | A readable name for [`SteamController.InputType`](SteamController.md#m-inputtype). |

### <a id="m-handle"></a>Handle

```csharp
public ulong Handle { get; init; }
```

Steam's handle.

**Type:** `ulong`

### <a id="m-index"></a>Index

```csharp
public int Index { get; init; }
```

1-based number, the lowest free one when the controller connected.

**Type:** `int`

### <a id="m-inputtype"></a>InputType

```csharp
public int InputType { get; init; }
```

Steam's input type number (3 is an Xbox One pad, 13 a PS5 pad, 14 the Steam Deck).

**Type:** `int`

### <a id="m-typename"></a>TypeName

```csharp
public string TypeName { get; }
```

A readable name for [`SteamController.InputType`](SteamController.md#m-inputtype).

**Type:** `string`
