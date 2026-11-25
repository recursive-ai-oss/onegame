[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# EnvironmentSlot enum

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public enum EnvironmentSlot : int
```

Which environment of the scene an adopted [`EnvironmentLoad`](../../OneGame.Render3D/OneGame.Render3D/EnvironmentLoad.md) replaces.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-map"></a>`Map` | `0` | The scene's `Environment.Map`: the picture changes at once when the load is ready. |
| <a id="m-nextmap"></a>`NextMap` | `1` | The scene's `Environment.NextMap`: fade to it by raising `Environment.Blend`. |
