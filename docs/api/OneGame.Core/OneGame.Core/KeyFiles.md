[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# KeyFiles record

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`KeyFiles`](KeyFiles.md)&gt;

```csharp
public sealed record KeyFiles : IEquatable<KeyFiles>
```

The files of one named key: the private key file and the one-line public key file beside it.

## Constructors

| Member | Description |
| --- | --- |
| [`KeyFiles(string, string, string, PeerKey, bool)`](#m-ctor-string-string-string-peerkey-bool) | The files of one named key: the private key file and the one-line public key file beside it. |

### <a id="m-ctor-string-string-string-peerkey-bool"></a>KeyFiles(string, string, string, PeerKey, bool)

```csharp
public KeyFiles(string Name, string PrivateFile, string PublicFile, PeerKey PublicKey, bool Created);
```

The files of one named key: the private key file and the one-line public key file beside it.

**Parameters**

- `Name` (`string`): The name the files are called after.
- `PrivateFile` (`string`): The key file (`name.key`); a secret.
- `PublicFile` (`string`): The public key file (`name.pub`), safe to share and to ship.
- `PublicKey` ([`PeerKey`](PeerKey.md)): The public key.
- `Created` (`bool`): True when this call made the files, false when they already existed and were read.

## Properties

| Member | Description |
| --- | --- |
| [`Created`](#m-created) | True when this call made the files, false when they already existed and were read. |
| [`Name`](#m-name) | The name the files are called after. |
| [`PrivateFile`](#m-privatefile) | The key file (`name.key`); a secret. |
| [`PublicFile`](#m-publicfile) | The public key file (`name.pub`), safe to share and to ship. |
| [`PublicKey`](#m-publickey) | The public key. |

### <a id="m-created"></a>Created

```csharp
public bool Created { get; init; }
```

True when this call made the files, false when they already existed and were read.

**Type:** `bool`

### <a id="m-name"></a>Name

```csharp
public string Name { get; init; }
```

The name the files are called after.

**Type:** `string`

### <a id="m-privatefile"></a>PrivateFile

```csharp
public string PrivateFile { get; init; }
```

The key file (`name.key`); a secret.

**Type:** `string`

### <a id="m-publicfile"></a>PublicFile

```csharp
public string PublicFile { get; init; }
```

The public key file (`name.pub`), safe to share and to ship.

**Type:** `string`

### <a id="m-publickey"></a>PublicKey

```csharp
public PeerKey PublicKey { get; init; }
```

The public key.

**Type:** [`PeerKey`](PeerKey.md)
