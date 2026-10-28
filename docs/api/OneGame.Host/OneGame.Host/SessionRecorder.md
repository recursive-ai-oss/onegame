[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# SessionRecorder class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class SessionRecorder
```

Records a session, following the game across world replacements and state patches. Create one with [`GameInstance.StartSessionRecording()`](GameInstance.md#m-startsessionrecording-recordingoptions).

## Methods

| Member | Description |
| --- | --- |
| [`Stop()`](#m-stop) | Ends the recording and returns the session. |

### <a id="m-stop"></a>Stop()

```csharp
public SessionRecording Stop();
```

Ends the recording and returns the session. A session in which nothing ran still has one (empty) segment.

**Returns:** [`SessionRecording`](SessionRecording.md)
