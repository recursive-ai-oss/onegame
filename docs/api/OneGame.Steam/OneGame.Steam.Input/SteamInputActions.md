[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputActions class

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public sealed class SteamInputActions
```

What a game declares to Steam Input: action sets with their actions. The same declaration drives the code (action state, glyphs, the virtual gamepad) and writes the in-game actions file (`game_actions_<appid>.vdf`) Steam needs, so the file and the code cannot disagree. A game keeps the generated file checked in and asserts [`SteamInputActions.Matches()`](SteamInputActions.md#m-matches-string-string) in a test.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamInputActions()`](#m-ctor) |  |

### <a id="m-ctor"></a>SteamInputActions()

```csharp
public SteamInputActions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Localization`](#m-localization) | English texts for Steam's configurator are the titles; other languages can be added here (Steam's API language names, e.g. `german`), token to text. |
| [`MajorRevision`](#m-majorrevision) | The file's major revision; bump it when action sets change so Steam replaces players' older bindings. |
| [`MinorRevision`](#m-minorrevision) | The file's minor revision, for added optional actions or text. |
| [`Sets`](#m-sets) | The sets in declaration order. |

### <a id="m-localization"></a>Localization

```csharp
public Dictionary<string, Dictionary<string, string>> Localization { get; }
```

English texts for Steam's configurator are the titles; other languages can be added here (Steam's API language names, e.g. `german`), token to text.

**Type:** [`Dictionary`](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2)`<string,` [`Dictionary`](https://learn.microsoft.com/dotnet/api/system.collections.generic.dictionary-2)`<string, string>>`

### <a id="m-majorrevision"></a>MajorRevision

```csharp
public int MajorRevision { get; init; }
```

The file's major revision; bump it when action sets change so Steam replaces players' older bindings. Emitted when above zero.

**Type:** `int`

### <a id="m-minorrevision"></a>MinorRevision

```csharp
public int MinorRevision { get; init; }
```

The file's minor revision, for added optional actions or text. Emitted when above zero.

**Type:** `int`

### <a id="m-sets"></a>Sets

```csharp
public IReadOnlyList<SteamInputSet> Sets { get; }
```

The sets in declaration order. The first is the default set.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamInputSet`](SteamInputSet.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`AllActions()`](#m-allactions) | Every declared action name, in declaration order, with its kind. |
| [`FindSet(string)`](#m-findset-string) | Finds a set by name. |
| [`Matches(string, out string?)`](#m-matches-string-string) | Checks that a checked-in actions file is what these declarations generate (line ends are ignored). |
| [`Set(string, string, Action<SteamInputSet>)`](#m-set-string-string-action-steaminputset) | Declares an action set. |
| [`SetForScreen(string?)`](#m-setforscreen-string) | The set to activate while a game screen is open: the set that lists the screen, else the first (default) set. |
| [`ToVdf()`](#m-tovdf) | Writes the in-game actions file Steam reads (Valve's KeyValues text format) for these declarations. |
| [`WriteVdf(string)`](#m-writevdf-string) | Writes [`SteamInputActions.ToVdf()`](SteamInputActions.md#m-tovdf) to a file. |

### <a id="m-allactions"></a>AllActions()

```csharp
public IEnumerable<(string Name, SteamActionKind Kind, string SetName)> AllActions();
```

Every declared action name, in declaration order, with its kind.

**Returns:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<(string Name,` [`SteamActionKind`](SteamActionKind.md) `Kind, string SetName)>` — Name and kind pairs.

### <a id="m-findset-string"></a>FindSet(string)

```csharp
public SteamInputSet? FindSet(string name);
```

Finds a set by name.

**Parameters**

- `name` (`string`): The set's name.

**Returns:** [`SteamInputSet`](SteamInputSet.md)? — The set, or null.

### <a id="m-matches-string-string"></a>Matches(string, out string?)

```csharp
public bool Matches(string existing, out string? difference);
```

Checks that a checked-in actions file is what these declarations generate (line ends are ignored).

**Parameters**

- `existing` (`string`): The file's text.
- `difference` (`string?`): The first differing line, as "line N: expected ..., found ...", or null.

**Returns:** `bool` — True when they agree.

### <a id="m-set-string-string-action-steaminputset"></a>Set(string, string, Action&lt;SteamInputSet&gt;)

```csharp
public SteamInputActions Set(string name, string title, Action<SteamInputSet> build);
```

Declares an action set.

**Parameters**

- `name` (`string`): Letters, digits and underscores, starting with a letter; unique.
- `title` (`string`): Text for the configurator.
- `build` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)&lt;[`SteamInputSet`](SteamInputSet.md)&gt;): Adds the set's actions.

**Returns:** [`SteamInputActions`](SteamInputActions.md) — This declaration.

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The name is malformed or already used.

### <a id="m-setforscreen-string"></a>SetForScreen(string?)

```csharp
public SteamInputSet? SetForScreen(string? screen);
```

The set to activate while a game screen is open: the set that lists the screen, else the first (default) set.

**Parameters**

- `screen` (`string?`): The screen name, or null.

**Returns:** [`SteamInputSet`](SteamInputSet.md)? — The set, or null when nothing is declared.

### <a id="m-tovdf"></a>ToVdf()

```csharp
public string ToVdf();
```

Writes the in-game actions file Steam reads (Valve's KeyValues text format) for these declarations. The output is deterministic.

**Returns:** `string` — The file's text, with `\n` line ends.

### <a id="m-writevdf-string"></a>WriteVdf(string)

```csharp
public void WriteVdf(string path);
```

Writes [`SteamInputActions.ToVdf()`](SteamInputActions.md#m-tovdf) to a file.

**Parameters**

- `path` (`string`): The file, usually `game_actions_<appid>.vdf`.
