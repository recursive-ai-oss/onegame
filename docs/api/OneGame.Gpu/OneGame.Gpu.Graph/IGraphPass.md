[API reference](../../index.md) › [OneGame.Gpu](../index.md) › OneGame.Gpu.Graph

# IGraphPass interface

**Namespace:** `OneGame.Gpu.Graph`  
**Package:** [`RecursiveAI.OneGame.Gpu`](../index.md)

```csharp
public interface IGraphPass
```

A pass of the render graph. Keep one instance per pass and reuse it every frame: a graph built from such objects allocates nothing per frame.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | Name shown in reports and timings. |

### <a id="m-name"></a>Name

```csharp
string Name { get; }
```

Name shown in reports and timings.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Execute(PassContext)`](#m-execute-passcontext) | Records the pass's commands. |
| [`Setup(PassBuilder)`](#m-setup-passbuilder) | Declares what the pass reads, writes and renders into. |

### <a id="m-execute-passcontext"></a>Execute(PassContext)

```csharp
void Execute(PassContext context);
```

Records the pass's commands. Runs only if the pass was not culled.

**Parameters**

- `context` ([`PassContext`](PassContext.md))

### <a id="m-setup-passbuilder"></a>Setup(PassBuilder)

```csharp
void Setup(PassBuilder builder);
```

Declares what the pass reads, writes and renders into. Runs every frame while the graph is built, before anything executes.

**Parameters**

- `builder` ([`PassBuilder`](PassBuilder.md))
