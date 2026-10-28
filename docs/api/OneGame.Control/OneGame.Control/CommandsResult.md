[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# CommandsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CommandsResult`](CommandsResult.md)&gt;

```csharp
public sealed record CommandsResult : IEquatable<CommandsResult>
```

One entity type's commands, as `commands_list` reports them (shown with the first entity of that type).

## Constructors

| Member | Description |
| --- | --- |
| [`CommandsResult(string, string, string[])`](#m-ctor-string-string-string) | One entity type's commands, as `commands_list` reports them (shown with the first entity of that type). |

### <a id="m-ctor-string-string-string"></a>CommandsResult(string, string, string\[\])

```csharp
public CommandsResult(string Entity, string Type, string[] Commands);
```

One entity type's commands, as `commands_list` reports them (shown with the first entity of that type).

**Parameters**

- `Entity` (`string`): The entity, as `Type#id`.
- `Type` (`string`): The entity type name.
- `Commands` (`string[]`): Each command with its parameter signature.

## Properties

| Member | Description |
| --- | --- |
| [`Commands`](#m-commands) | Each command with its parameter signature. |
| [`Entity`](#m-entity) | The entity, as `Type#id`. |
| [`Type`](#m-type) | The entity type name. |

### <a id="m-commands"></a>Commands

```csharp
public string[] Commands { get; init; }
```

Each command with its parameter signature.

**Type:** `string[]`

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

The entity, as `Type#id`.

**Type:** `string`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

The entity type name.

**Type:** `string`
