[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# IEntropySource interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface IEntropySource
```

A source of unpredictable bytes, for platforms that supply their own (the browser's `crypto.getRandomValues`, say).

## Methods

| Member | Description |
| --- | --- |
| [`Fill(Span<byte>)`](#m-fill-span-byte) | Fills `buffer` with unpredictable bytes. |

### <a id="m-fill-span-byte"></a>Fill(Span&lt;byte&gt;)

```csharp
void Fill(Span<byte> buffer);
```

Fills `buffer` with unpredictable bytes. May be called from any thread.

**Parameters**

- `buffer` ([`Span`](https://learn.microsoft.com/dotnet/api/system.span-1)`<byte>`)
