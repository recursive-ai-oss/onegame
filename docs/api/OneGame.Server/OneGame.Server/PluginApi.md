[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginApi static class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public static class PluginApi
```

The version of the plugin API a host speaks. A plugin records the version it was built against and the host refuses it when that is outside [`PluginApi.MinimumVersion`](PluginApi.md#m-minimumversion)..[`PluginApi.Version`](PluginApi.md#m-version).

## Fields

| Member | Description |
| --- | --- |
| [`MinimumVersion`](#m-minimumversion) | The oldest API version this host still accepts. |
| [`Version`](#m-version) | The newest API version this host implements. |

### <a id="m-minimumversion"></a>MinimumVersion

```csharp
public const int MinimumVersion = 1;
```

The oldest API version this host still accepts.

**Type:** `int`

### <a id="m-version"></a>Version

```csharp
public const int Version = 1;
```

The newest API version this host implements.

**Type:** `int`
