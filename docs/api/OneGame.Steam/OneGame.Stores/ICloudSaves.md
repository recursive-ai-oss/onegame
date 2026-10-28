[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# ICloudSaves interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface ICloudSaves
```

Cloud-synced save files. Names are relative paths with forward slashes, at most 260 bytes.

## Fields

| Member | Description |
| --- | --- |
| [`MaxFileBytes`](#m-maxfilebytes) | Largest file the store accepts, in bytes. |

### <a id="m-maxfilebytes"></a>MaxFileBytes

```csharp
public const int MaxFileBytes = 104857600;
```

Largest file the store accepts, in bytes.

**Type:** `int`

## Properties

| Member | Description |
| --- | --- |
| [`IsEnabled`](#m-isenabled) | True when the player has cloud saves turned on for the account and for this game. |

### <a id="m-isenabled"></a>IsEnabled

```csharp
bool IsEnabled { get; }
```

True when the player has cloud saves turned on for the account and for this game.

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`Delete(string)`](#m-delete-string) | Deletes a file from the cloud and the local copy. |
| [`Exists(string)`](#m-exists-string) | Tests whether a file exists. |
| [`List()`](#m-list) | Lists the files the store tracks for this game. |
| [`Read(string, out byte[], int)`](#m-read-string-byte-int) | Reads a whole file. |
| [`TryGetQuota(out CloudQuota)`](#m-trygetquota-cloudquota) | Reads the player's cloud quota. |
| [`Write(string, ReadOnlySpan<byte>)`](#m-write-string-readonlyspan-byte) | Writes a file, replacing any existing one. |

### <a id="m-delete-string"></a>Delete(string)

```csharp
StoreResult Delete(string name);
```

Deletes a file from the cloud and the local copy.

**Parameters**

- `name` (`string`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-exists-string"></a>Exists(string)

```csharp
bool Exists(string name);
```

Tests whether a file exists.

**Parameters**

- `name` (`string`)

**Returns:** `bool`

### <a id="m-list"></a>List()

```csharp
IReadOnlyList<CloudFile> List();
```

Lists the files the store tracks for this game.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`CloudFile`](CloudFile.md)&gt;

### <a id="m-read-string-byte-int"></a>Read(string, out byte\[\], int)

```csharp
StoreResult Read(string name, out byte[] data, int maxBytes = 104857600);
```

Reads a whole file. The size is checked against `maxBytes` before any buffer is allocated.

**Parameters**

- `name` (`string`)
- `data` (`byte[]`)
- `maxBytes` (`int`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-trygetquota-cloudquota"></a>TryGetQuota(out CloudQuota)

```csharp
StoreResult TryGetQuota(out CloudQuota quota);
```

Reads the player's cloud quota.

**Parameters**

- `quota` ([`CloudQuota`](CloudQuota.md))

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-write-string-readonlyspan-byte"></a>Write(string, ReadOnlySpan&lt;byte&gt;)

```csharp
StoreResult Write(string name, ReadOnlySpan<byte> data);
```

Writes a file, replacing any existing one.

**Parameters**

- `name` (`string`)
- `data` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`)

**Returns:** [`StoreResult`](StoreResult.md)
