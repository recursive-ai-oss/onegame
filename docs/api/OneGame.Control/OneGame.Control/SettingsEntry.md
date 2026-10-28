[API reference](../../index.md) › [OneGame.Control](../index.md) › OneGame.Control

# SettingsEntry record

**Namespace:** `OneGame.Control`  
**Package:** [`RecursiveAI.OneGame.Control`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SettingsEntry`](SettingsEntry.md)&gt;

```csharp
public sealed record SettingsEntry : IEquatable<SettingsEntry>
```

One settings file in the `saves` report.

## Constructors

| Member | Description |
| --- | --- |
| [`SettingsEntry(string, string, string, string[], string)`](#m-ctor-string-string-string-string-string) | One settings file in the `saves` report. |

### <a id="m-ctor-string-string-string-string-string"></a>SettingsEntry(string, string, string, string\[\], string)

```csharp
public SettingsEntry(string File, string Status, string Reason, string[] Notes, string Text);
```

One settings file in the `saves` report.

**Parameters**

- `File` (`string`): The file name in the store.
- `Status` (`string`): `loaded`, `missing` or `defaulted`.
- `Reason` (`string`): Why everything is at its default, or empty.
- `Notes` (`string[]`): One line per field that took its default.
- `Text` (`string`): The current values, one `key = value` line each.

## Properties

| Member | Description |
| --- | --- |
| [`File`](#m-file) | The file name in the store. |
| [`Notes`](#m-notes) | One line per field that took its default. |
| [`Reason`](#m-reason) | Why everything is at its default, or empty. |
| [`Status`](#m-status) | `loaded`, `missing` or `defaulted`. |
| [`Text`](#m-text) | The current values, one `key = value` line each. |

### <a id="m-file"></a>File

```csharp
public string File { get; init; }
```

The file name in the store.

**Type:** `string`

### <a id="m-notes"></a>Notes

```csharp
public string[] Notes { get; init; }
```

One line per field that took its default.

**Type:** `string[]`

### <a id="m-reason"></a>Reason

```csharp
public string Reason { get; init; }
```

Why everything is at its default, or empty.

**Type:** `string`

### <a id="m-status"></a>Status

```csharp
public string Status { get; init; }
```

`loaded`, `missing` or `defaulted`.

**Type:** `string`

### <a id="m-text"></a>Text

```csharp
public string Text { get; init; }
```

The current values, one `key = value` line each.

**Type:** `string`
