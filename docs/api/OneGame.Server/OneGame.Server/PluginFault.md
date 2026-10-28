[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginFault record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PluginFault`](PluginFault.md)&gt;

```csharp
public sealed record PluginFault : IEquatable<PluginFault>
```

What went wrong inside a plugin. Text only, so a record never keeps a plugin's types alive.

## Constructors

| Member | Description |
| --- | --- |
| [`PluginFault(string, string, string, string, long)`](#m-ctor-string-string-string-string-long) | What went wrong inside a plugin. |

### <a id="m-ctor-string-string-string-string-long"></a>PluginFault(string, string, string, string, long)

```csharp
public PluginFault(string Phase, string Type, string Message, string Detail, long Tick);
```

What went wrong inside a plugin. Text only, so a record never keeps a plugin's types alive.

**Parameters**

- `Phase` (`string`): `load`, `seed`, `tick` (also commands and entity logic), `unload` or `reload`.
- `Type` (`string`): The exception type name.
- `Message` (`string`): The exception message.
- `Detail` (`string`): The first lines of the stack trace.
- `Tick` (`long`): The server tick when it happened.

## Properties

| Member | Description |
| --- | --- |
| [`Detail`](#m-detail) | The first lines of the stack trace. |
| [`Message`](#m-message) | The exception message. |
| [`Phase`](#m-phase) | `load`, `seed`, `tick` (also commands and entity logic), `unload` or `reload`. |
| [`Tick`](#m-tick) | The server tick when it happened. |
| [`Type`](#m-type) | The exception type name. |

### <a id="m-detail"></a>Detail

```csharp
public string Detail { get; init; }
```

The first lines of the stack trace.

**Type:** `string`

### <a id="m-message"></a>Message

```csharp
public string Message { get; init; }
```

The exception message.

**Type:** `string`

### <a id="m-phase"></a>Phase

```csharp
public string Phase { get; init; }
```

`load`, `seed`, `tick` (also commands and entity logic), `unload` or `reload`.

**Type:** `string`

### <a id="m-tick"></a>Tick

```csharp
public long Tick { get; init; }
```

The server tick when it happened.

**Type:** `long`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

The exception type name.

**Type:** `string`
