[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# CommandsResult record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`CommandsResult`](CommandsResult.md)&gt;

```csharp
public sealed record CommandsResult : IEquatable<CommandsResult>
```

One entity type's commands.

## Constructors

| Member | Description |
| --- | --- |
| [`CommandsResult(string, string, string[])`](#m-ctor-string-string-string) | One entity type's commands. |

### <a id="m-ctor-string-string-string"></a>CommandsResult(string, string, string\[\])

```csharp
public CommandsResult(string Entity, string Type, string[] Commands);
```

One entity type's commands.

**Parameters**

- `Entity` (`string`)
- `Type` (`string`)
- `Commands` (`string[]`)

## Properties

| Member | Description |
| --- | --- |
| [`Commands`](#m-commands) |  |
| [`Entity`](#m-entity) |  |
| [`Type`](#m-type) |  |

### <a id="m-commands"></a>Commands

```csharp
public string[] Commands { get; init; }
```

**Type:** `string[]`

### <a id="m-entity"></a>Entity

```csharp
public string Entity { get; init; }
```

**Type:** `string`

### <a id="m-type"></a>Type

```csharp
public string Type { get; init; }
```

**Type:** `string`
