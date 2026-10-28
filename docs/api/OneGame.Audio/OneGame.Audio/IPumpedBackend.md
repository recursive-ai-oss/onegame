[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# IPumpedBackend interface

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)

```csharp
public interface IPumpedBackend
```

Implemented by backends of pumped devices: the output hands over the function the page's timer must call.

## Methods

| Member | Description |
| --- | --- |
| [`SetPump(Action)`](#m-setpump-action) | Registers `pump` (the output's [`AudioOutput.Pump()`](AudioOutput.md#m-pump)) to be called every few milliseconds on the page's thread. |

### <a id="m-setpump-action"></a>SetPump(Action)

```csharp
void SetPump(Action pump);
```

Registers `pump` (the output's [`AudioOutput.Pump()`](AudioOutput.md#m-pump)) to be called every few milliseconds on the page's thread.

**Parameters**

- `pump` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action))
