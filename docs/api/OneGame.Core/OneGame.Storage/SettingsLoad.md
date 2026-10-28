[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# SettingsLoad record

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SettingsLoad`](SettingsLoad.md)&gt;

```csharp
public sealed record SettingsLoad : IEquatable<SettingsLoad>
```

What [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load) found.

## Constructors

| Member | Description |
| --- | --- |
| [`SettingsLoad(SettingsStatus, string, IReadOnlyList<string>, int)`](#m-ctor-settingsstatus-string-ireadonlylist-string-int) | What [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load) found. |

### <a id="m-ctor-settingsstatus-string-ireadonlylist-string-int"></a>SettingsLoad(SettingsStatus, string, IReadOnlyList&lt;string&gt;, int)

```csharp
public SettingsLoad(SettingsStatus Status, string Reason, IReadOnlyList<string> Notes, int FileVersion);
```

What [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load) found.

**Parameters**

- `Status` ([`SettingsStatus`](SettingsStatus.md)): The outcome.
- `Reason` (`string`): Why everything is at its default, or an empty string.
- `Notes` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): One line for each field that took its default, and for each key in the file the schema does not know.
- `FileVersion` (`int`): The version the file was saved with, or 0 when none was read.

## Properties

| Member | Description |
| --- | --- |
| [`Clean`](#m-clean) | True when nothing was wrong: the file was read in full, or there is no file yet. |
| [`FileVersion`](#m-fileversion) | The version the file was saved with, or 0 when none was read. |
| [`Notes`](#m-notes) | One line for each field that took its default, and for each key in the file the schema does not know. |
| [`Reason`](#m-reason) | Why everything is at its default, or an empty string. |
| [`Status`](#m-status) | The outcome. |

### <a id="m-clean"></a>Clean

```csharp
public bool Clean { get; }
```

True when nothing was wrong: the file was read in full, or there is no file yet.

**Type:** `bool`

### <a id="m-fileversion"></a>FileVersion

```csharp
public int FileVersion { get; init; }
```

The version the file was saved with, or 0 when none was read.

**Type:** `int`

### <a id="m-notes"></a>Notes

```csharp
public IReadOnlyList<string> Notes { get; init; }
```

One line for each field that took its default, and for each key in the file the schema does not know.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-reason"></a>Reason

```csharp
public string Reason { get; init; }
```

Why everything is at its default, or an empty string.

**Type:** `string`

### <a id="m-status"></a>Status

```csharp
public SettingsStatus Status { get; init; }
```

The outcome.

**Type:** [`SettingsStatus`](SettingsStatus.md)
