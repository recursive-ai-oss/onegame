[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# DownReport record

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`DownReport`](DownReport.md)&gt;

```csharp
public sealed record DownReport : IEquatable<DownReport>
```

What `cluster down` did.

## Constructors

| Member | Description |
| --- | --- |
| [`DownReport(string[], string[], string[])`](#m-ctor-string-string-string) | What `cluster down` did. |

### <a id="m-ctor-string-string-string"></a>DownReport(string\[\], string\[\], string\[\])

```csharp
public DownReport(string[] Stopped, string[] Killed, string[] Alive);
```

What `cluster down` did.

**Parameters**

- `Stopped` (`string[]`): Nodes that stopped when asked.
- `Killed` (`string[]`): Nodes that had to be killed.
- `Alive` (`string[]`): Nodes still alive afterwards: empty unless something is badly wrong.

## Properties

| Member | Description |
| --- | --- |
| [`Alive`](#m-alive) | Nodes still alive afterwards: empty unless something is badly wrong. |
| [`Killed`](#m-killed) | Nodes that had to be killed. |
| [`Stopped`](#m-stopped) | Nodes that stopped when asked. |

### <a id="m-alive"></a>Alive

```csharp
public string[] Alive { get; init; }
```

Nodes still alive afterwards: empty unless something is badly wrong.

**Type:** `string[]`

### <a id="m-killed"></a>Killed

```csharp
public string[] Killed { get; init; }
```

Nodes that had to be killed.

**Type:** `string[]`

### <a id="m-stopped"></a>Stopped

```csharp
public string[] Stopped { get; init; }
```

Nodes that stopped when asked.

**Type:** `string[]`
