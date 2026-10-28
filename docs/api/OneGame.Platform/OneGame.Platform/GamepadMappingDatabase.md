[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GamepadMappingDatabase class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class GamepadMappingDatabase
```

A set of [`GamepadMapping`](GamepadMapping.md) entries looked up by device GUID.

## Constructors

| Member | Description |
| --- | --- |
| [`GamepadMappingDatabase()`](#m-ctor) |  |

### <a id="m-ctor"></a>GamepadMappingDatabase()

```csharp
public GamepadMappingDatabase();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of entries. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of entries.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Add(GamepadMapping)`](#m-add-gamepadmapping) | Adds one entry. |
| [`AddText(string, string?)`](#m-addtext-string-string) | Adds every valid entry in `text` (one per line). |
| [`Find(string)`](#m-find-string) | Finds the entry for a device: an exact GUID match first, then one that differs only in the name checksum and version fields. |

### <a id="m-add-gamepadmapping"></a>Add(GamepadMapping)

```csharp
public void Add(GamepadMapping mapping);
```

Adds one entry.

**Parameters**

- `mapping` ([`GamepadMapping`](GamepadMapping.md))

### <a id="m-addtext-string-string"></a>AddText(string, string?)

```csharp
public int AddText(string text, string? platform = null);
```

Adds every valid entry in `text` (one per line). Entries for other platforms are skipped. Later entries win.

**Parameters**

- `text` (`string`)
- `platform` (`string?`)

**Returns:** `int` — How many entries were added.

### <a id="m-find-string"></a>Find(string)

```csharp
public GamepadMapping? Find(string deviceId);
```

Finds the entry for a device: an exact GUID match first, then one that differs only in the name checksum and version fields.

**Parameters**

- `deviceId` (`string`)

**Returns:** [`GamepadMapping`](GamepadMapping.md)?
