[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Storage

# SaveSlotOptions class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public sealed class SaveSlotOptions
```

Options for [`SaveSlots`](SaveSlots.md).

## Constructors

| Member | Description |
| --- | --- |
| [`SaveSlotOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>SaveSlotOptions()

```csharp
public SaveSlotOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`AutosaveSlots`](#m-autosaveslots) | How many rotating autosave slots there are (default 3). |
| [`GameVersion`](#m-gameversion) | Your game's version text, recorded in each slot (at most 64 characters). |
| [`MaxSaveBytes`](#m-maxsavebytes) | The largest slot file read or written, in bytes (default 64 MiB). |
| [`Time`](#m-time) | The clock that stamps slots. |

### <a id="m-autosaveslots"></a>AutosaveSlots

```csharp
public int AutosaveSlots { get; init; }
```

How many rotating autosave slots there are (default 3).

**Type:** `int`

### <a id="m-gameversion"></a>GameVersion

```csharp
public string GameVersion { get; init; }
```

Your game's version text, recorded in each slot (at most 64 characters). Informational: migrations key off entity schema versions.

**Type:** `string`

### <a id="m-maxsavebytes"></a>MaxSaveBytes

```csharp
public int MaxSaveBytes { get; init; }
```

The largest slot file read or written, in bytes (default 64 MiB). Checked before anything is allocated.

**Type:** `int`

### <a id="m-time"></a>Time

```csharp
public TimeProvider Time { get; init; }
```

The clock that stamps slots. Replace it in tests to get the same bytes every run.

**Type:** [`TimeProvider`](https://learn.microsoft.com/dotnet/api/system.timeprovider)
