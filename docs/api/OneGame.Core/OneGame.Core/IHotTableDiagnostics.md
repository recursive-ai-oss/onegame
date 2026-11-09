[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# IHotTableDiagnostics interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface IHotTableDiagnostics
```

Optional tooling diagnostics for a table whose binary state has named fields. Not used by the tick or replica wire protocol.

## Methods

| Member | Description |
| --- | --- |
| [`CaptureDiagnosticSnapshot()`](#m-capturediagnosticsnapshot) | Captures an independent diagnostic snapshot for a recording checkpoint. |
| [`DescribeDifferences(ReadOnlySpan<byte>)`](#m-describedifferences-readonlyspan-byte) | Describes differences from a captured snapshot, naming rows and fields. |

### <a id="m-capturediagnosticsnapshot"></a>CaptureDiagnosticSnapshot()

```csharp
byte[] CaptureDiagnosticSnapshot();
```

Captures an independent diagnostic snapshot for a recording checkpoint. May allocate; callers opt in explicitly.

**Returns:** `byte[]`

### <a id="m-describedifferences-readonlyspan-byte"></a>DescribeDifferences(ReadOnlySpan&lt;byte&gt;)

```csharp
string DescribeDifferences(ReadOnlySpan<byte> expected);
```

Describes differences from a captured snapshot, naming rows and fields. The snapshot must come from the same table implementation.

**Parameters**

- `expected` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** `string`
