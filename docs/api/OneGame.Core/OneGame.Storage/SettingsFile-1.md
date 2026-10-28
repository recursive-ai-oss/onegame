[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# SettingsFile&lt;T&gt; class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISettingsFile`](ISettingsFile.md)

```csharp
public sealed class SettingsFile<T> : ISettingsFile where T : class, new()
```

A typed settings object kept in a store: load it once at start, change its properties, call [`SettingsFile<T>.Save()`](SettingsFile-1.md#m-save). A damaged or out-of-range file never throws; it falls back to defaults and says why in the [`SettingsLoad`](SettingsLoad.md).

**Type parameters**

- `T`: The settings class.

## Constructors

| Member | Description |
| --- | --- |
| [`SettingsFile(IByteStore, SettingsSchema<T>)`](#m-ctor-ibytestore-settingsschema-t) | Creates the settings for `schema` in `store`. |

### <a id="m-ctor-ibytestore-settingsschema-t"></a>SettingsFile(IByteStore, SettingsSchema&lt;T&gt;)

```csharp
public SettingsFile(IByteStore store, SettingsSchema<T> schema);
```

Creates the settings for `schema` in `store`. [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value) holds the defaults until [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load).

**Parameters**

- `store` ([`IByteStore`](IByteStore.md))
- `schema` ([`SettingsSchema`](SettingsSchema-1.md)`<T>`)

## Fields

| Member | Description |
| --- | --- |
| [`MaxBytes`](#m-maxbytes) | The largest settings file read, in bytes. |
| [`MaxTextBytes`](#m-maxtextbytes) | The longest text value, in UTF-8 bytes. |

### <a id="m-maxbytes"></a>MaxBytes

```csharp
public const int MaxBytes = 65536;
```

The largest settings file read, in bytes.

**Type:** `int`

### <a id="m-maxtextbytes"></a>MaxTextBytes

```csharp
public const int MaxTextBytes = 4096;
```

The longest text value, in UTF-8 bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`FileName`](#m-filename) | The file the settings are kept in. |
| [`LastLoad`](#m-lastload) | What the latest [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load) or [`SettingsFile<T>.Apply()`](SettingsFile-1.md#m-apply-readonlyspan-byte) found, or null before the first. |
| [`Schema`](#m-schema) | The schema. |
| [`Value`](#m-value) | The settings object. |

### <a id="m-filename"></a>FileName

```csharp
public string FileName { get; }
```

The file the settings are kept in.

**Type:** `string`

### <a id="m-lastload"></a>LastLoad

```csharp
public SettingsLoad? LastLoad { get; }
```

What the latest [`SettingsFile<T>.Load()`](SettingsFile-1.md#m-load) or [`SettingsFile<T>.Apply()`](SettingsFile-1.md#m-apply-readonlyspan-byte) found, or null before the first.

**Type:** [`SettingsLoad`](SettingsLoad.md)?

### <a id="m-schema"></a>Schema

```csharp
public SettingsSchema<T> Schema { get; }
```

The schema.

**Type:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`

### <a id="m-value"></a>Value

```csharp
public T Value { get; }
```

The settings object. It is the same instance for the life of this file, so a screen can hold on to it.

**Type:** `T`

## Methods

| Member | Description |
| --- | --- |
| [`Apply(ReadOnlySpan<byte>)`](#m-apply-readonlyspan-byte) | Applies settings bytes to [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value) without a store (the file's content, already read). |
| [`Load()`](#m-load) | Reads the file into [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value). |
| [`Reset()`](#m-reset) | Puts every field back to its default (the values of a new `T`) without touching the file. |
| [`Save()`](#m-save) | Writes [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value) to the store. |
| [`ToBytes()`](#m-tobytes) | The file's bytes for the current values. |
| [`ToText()`](#m-totext) | The values as `key = value` lines in schema order, for logs and the harness. |

### <a id="m-apply-readonlyspan-byte"></a>Apply(ReadOnlySpan&lt;byte&gt;)

```csharp
public SettingsLoad Apply(ReadOnlySpan<byte> bytes);
```

Applies settings bytes to [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value) without a store (the file's content, already read).

**Parameters**

- `bytes` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** [`SettingsLoad`](SettingsLoad.md)

### <a id="m-load"></a>Load()

```csharp
public SettingsLoad Load();
```

Reads the file into [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value). Never throws for bad content.

**Returns:** [`SettingsLoad`](SettingsLoad.md)

### <a id="m-reset"></a>Reset()

```csharp
public void Reset();
```

Puts every field back to its default (the values of a new `T`) without touching the file.

### <a id="m-save"></a>Save()

```csharp
public bool Save();
```

Writes [`SettingsFile<T>.Value`](SettingsFile-1.md#m-value) to the store. Returns false when the store could not write it (the old file stays).

**Returns:** `bool`

### <a id="m-tobytes"></a>ToBytes()

```csharp
public byte[] ToBytes();
```

The file's bytes for the current values.

**Returns:** `byte[]`

### <a id="m-totext"></a>ToText()

```csharp
public string ToText();
```

The values as `key = value` lines in schema order, for logs and the harness.

**Returns:** `string`
