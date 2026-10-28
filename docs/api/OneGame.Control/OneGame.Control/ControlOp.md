[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# ControlOp record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`ControlOp`](ControlOp.md)&gt;

```csharp
public sealed record ControlOp : IEquatable<ControlOp>
```

One operation of the control layer: the name every face uses, what it does, and its arguments as a JSON schema.

## Constructors

| Member | Description |
| --- | --- |
| [`ControlOp(string, string, string, bool)`](#m-ctor-string-string-string-bool) | One operation of the control layer: the name every face uses, what it does, and its arguments as a JSON schema. |

### <a id="m-ctor-string-string-string-bool"></a>ControlOp(string, string, string, bool)

```csharp
public ControlOp(string Name, string Description, string InputSchema, bool ChangesState);
```

One operation of the control layer: the name every face uses, what it does, and its arguments as a JSON schema.

**Parameters**

- `Name` (`string`): The operation name, also the MCP tool name.
- `Description` (`string`): What it does, for an agent that reads the tool list.
- `InputSchema` (`string`): JSON schema of the arguments object.
- `ChangesState` (`bool`): True when the operation can change the simulation (as opposed to only reading it).

## Properties

| Member | Description |
| --- | --- |
| [`ChangesState`](#m-changesstate) | True when the operation can change the simulation (as opposed to only reading it). |
| [`Description`](#m-description) | What it does, for an agent that reads the tool list. |
| [`InputSchema`](#m-inputschema) | JSON schema of the arguments object. |
| [`Name`](#m-name) | The operation name, also the MCP tool name. |

### <a id="m-changesstate"></a>ChangesState

```csharp
public bool ChangesState { get; init; }
```

True when the operation can change the simulation (as opposed to only reading it).

**Type:** `bool`

### <a id="m-description"></a>Description

```csharp
public string Description { get; init; }
```

What it does, for an agent that reads the tool list.

**Type:** `string`

### <a id="m-inputschema"></a>InputSchema

```csharp
public string InputSchema { get; init; }
```

JSON schema of the arguments object.

**Type:** `string`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The operation name, also the MCP tool name.

**Type:** `string`
