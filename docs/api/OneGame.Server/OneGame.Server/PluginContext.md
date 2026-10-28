[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# PluginContext class

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)

```csharp
public sealed class PluginContext
```

What a plugin sees while it loads: its settings, the scopes it is bound to, and the places to register types and systems.

## Properties

| Member | Description |
| --- | --- |
| [`IsReload`](#m-isreload) | True when this load replaces a running version. |
| [`Name`](#m-name) | The plugin's configured name. |
| [`Scopes`](#m-scopes) | The scopes the plugin is bound to. |

### <a id="m-isreload"></a>IsReload

```csharp
public bool IsReload { get; }
```

True when this load replaces a running version.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The plugin's configured name.

**Type:** `string`

### <a id="m-scopes"></a>Scopes

```csharp
public IReadOnlyList<ScopeId> Scopes { get; }
```

The scopes the plugin is bound to.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ScopeId`](../../OneGame.Sync/OneGame.Sync/ScopeId.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`AddEntity<T>()`](#m-addentity-t) | Registers an entity type the plugin provides. |
| [`AddSystem(ISimSystem)`](#m-addsystem-isimsystem) | Adds a system to every bound scope. |
| [`Log(string)`](#m-log-string) | Writes a line to the server log, tagged with the plugin name. |
| [`Setting(string)`](#m-setting-string) | A setting from the plugin's `settings` block in the server configuration, or null. |
| [`SettingInt(string, int)`](#m-settingint-string-int) | A whole-number setting, or `fallback` when it is absent. |

### <a id="m-addentity-t"></a>AddEntity&lt;T&gt;()

```csharp
public void AddEntity<T>() where T : Entity, new();
```

Registers an entity type the plugin provides. Its commands become available on the bound scopes, and saves containing it restore through the type's migrations.

**Type parameters**

- `T`: A generated entity class with a public parameterless constructor.

### <a id="m-addsystem-isimsystem"></a>AddSystem(ISimSystem)

```csharp
public void AddSystem(ISimSystem system);
```

Adds a system to every bound scope. Systems run after all entities, in plugin order then registration order. Keep their state in entities or tables.

**Parameters**

- `system` ([`ISimSystem`](../../OneGame.Core/OneGame.Core/ISimSystem.md)): The system.

### <a id="m-log-string"></a>Log(string)

```csharp
public void Log(string message);
```

Writes a line to the server log, tagged with the plugin name.

**Parameters**

- `message` (`string`): The text.

### <a id="m-setting-string"></a>Setting(string)

```csharp
public string? Setting(string key);
```

A setting from the plugin's `settings` block in the server configuration, or null.

**Parameters**

- `key` (`string`): The setting name.

**Returns:** `string?`

### <a id="m-settingint-string-int"></a>SettingInt(string, int)

```csharp
public int SettingInt(string key, int fallback);
```

A whole-number setting, or `fallback` when it is absent.

**Parameters**

- `key` (`string`): The setting name.
- `fallback` (`int`): The value when the setting is absent.

**Returns:** `int`

**Exceptions**

- [`PluginException`](PluginException.md): The setting is present but not a whole number.
