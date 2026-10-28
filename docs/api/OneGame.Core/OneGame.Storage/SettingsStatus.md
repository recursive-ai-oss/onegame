[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# SettingsStatus enum

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public enum SettingsStatus : int
```

How a settings load went.

## Members

| Name | Value | Description |
| --- | --- | --- |
| <a id="m-loaded"></a>`Loaded` | `0` | The file was read; fields that were missing or invalid took their defaults (see [`SettingsLoad.Notes`](SettingsLoad.md#m-notes)). |
| <a id="m-missing"></a>`Missing` | `1` | There is no settings file yet: everything is at its default. |
| <a id="m-defaulted"></a>`Defaulted` | `2` | The file is damaged, from another program, or too large: everything is at its default and the file was left alone. |
