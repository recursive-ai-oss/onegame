[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# ISettingsFile interface

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public interface ISettingsFile
```

The type-independent face of a [`SettingsFile<T>`](SettingsFile-1.md), for tools that list a game's settings.

## Properties

| Member | Description |
| --- | --- |
| [`FileName`](#m-filename) | The file the settings are kept in. |
| [`LastLoad`](#m-lastload) | What the latest load found, or null before the first. |

### <a id="m-filename"></a>FileName

```csharp
string FileName { get; }
```

The file the settings are kept in.

**Type:** `string`

### <a id="m-lastload"></a>LastLoad

```csharp
SettingsLoad? LastLoad { get; }
```

What the latest load found, or null before the first.

**Type:** [`SettingsLoad`](SettingsLoad.md)?

## Methods

| Member | Description |
| --- | --- |
| [`Load()`](#m-load) | Reads the file again. |
| [`Save()`](#m-save) | Writes the current values. |
| [`ToText()`](#m-totext) | The values as `key = value` lines. |

### <a id="m-load"></a>Load()

```csharp
SettingsLoad Load();
```

Reads the file again.

**Returns:** [`SettingsLoad`](SettingsLoad.md)

### <a id="m-save"></a>Save()

```csharp
bool Save();
```

Writes the current values.

**Returns:** `bool`

### <a id="m-totext"></a>ToText()

```csharp
string ToText();
```

The values as `key = value` lines.

**Returns:** `string`
