[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# Determinism.Drive delegate

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)  
**Declared in:** [`Determinism`](Determinism.md)

```csharp
public delegate void Determinism.Drive(TestClient client, int frame);
```

Does something to the game before frame number `frame` runs: send a command, click, press a key.
