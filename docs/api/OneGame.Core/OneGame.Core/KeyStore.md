[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# KeyStore static class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class KeyStore
```

Makes and finds key files. `onegame keys` is this class on the command line. Development tools use [`KeyStore.Ensure()`](KeyStore.md#m-ensure-string-string-bool) with [`KeyStore.DevelopmentFolder()`](KeyStore.md#m-developmentfolder-string), so a developer never has to make a key by hand, and a development key stays in a per-user folder outside the game, where no export or commit picks it up.

## Methods

| Member | Description |
| --- | --- |
| [`Create(string, string, bool, bool)`](#m-create-string-string-bool-bool) | Makes `name.key` and `name.pub` in `folder`. |
| [`DevelopmentFolder(string)`](#m-developmentfolder-string) | The per-user folder development keys live in for one project (or cluster): `ONEGAME_KEYS` if set, else the user's local application data. |
| [`Ensure(string, string, bool)`](#m-ensure-string-string-bool) | Reads `name.key` in `folder`, making it first when it is missing. |

### <a id="m-create-string-string-bool-bool"></a>Create(string, string, bool, bool)

```csharp
public static KeyFiles Create(string folder, string name, bool force = false, bool development = false);
```

Makes `name.key` and `name.pub` in `folder`.

**Parameters**

- `folder` (`string`): Where to write; created if missing. A `.gitignore` that ignores `*.key` is added when the folder has none.
- `name` (`string`): The file name without extension.
- `force` (`bool`): Replace existing files. Without it, an existing key is never overwritten.
- `development` (`bool`): Mark the key as a development key.

**Returns:** [`KeyFiles`](KeyFiles.md)

**Exceptions**

- [`IOException`](https://learn.microsoft.com/dotnet/api/system.io.ioexception): The key file exists and `force` is false.

### <a id="m-developmentfolder-string"></a>DevelopmentFolder(string)

```csharp
public static string DevelopmentFolder(string scope);
```

The per-user folder development keys live in for one project (or cluster): `ONEGAME_KEYS` if set, else the user's local application data. Never inside the project, so an export or a commit cannot pick a key up.

**Parameters**

- `scope` (`string`): What the keys belong to, usually the project folder's full path; different scopes get different folders.

**Returns:** `string`

### <a id="m-ensure-string-string-bool"></a>Ensure(string, string, bool)

```csharp
public static KeyFiles Ensure(string folder, string name, bool development = false);
```

Reads `name.key` in `folder`, making it first when it is missing.

**Parameters**

- `folder` (`string`): The key folder.
- `name` (`string`): The file name without extension.
- `development` (`bool`): Mark a key made by this call as a development key.

**Returns:** [`KeyFiles`](KeyFiles.md)
