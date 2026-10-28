[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# NetKeyPair class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class NetKeyPair
```

An X25519 key pair: a server's identity or a client's. **The private half is a secret.** Whoever holds a server's private key can impersonate that server to every client that trusts its public key, and whoever holds a client's can act as that player. Keep private keys out of source control and out of client builds; only [`NetKeyPair.PublicKey`](NetKeyPair.md#m-publickey) is meant to be shared.

## Properties

| Member | Description |
| --- | --- |
| [`IsDevelopment`](#m-isdevelopment) | True for a key made by the development tools (`onegame run`, clusters, samples). |
| [`PublicKey`](#m-publickey) | The shareable half. |

### <a id="m-isdevelopment"></a>IsDevelopment

```csharp
public bool IsDevelopment { get; }
```

True for a key made by the development tools (`onegame run`, clusters, samples). The mark is in the key file, and a server refuses to start with such a key unless it is told it is a development run, so a dev key cannot quietly become a production key.

**Type:** `bool`

### <a id="m-publickey"></a>PublicKey

```csharp
public PeerKey PublicKey { get; }
```

The shareable half.

**Type:** [`PeerKey`](PeerKey.md)

## Methods

| Member | Description |
| --- | --- |
| [`DeriveFor(PeerKey)`](#m-derivefor-peerkey) | A key pair derived from this one for one server: HKDF-SHA-256 of the private key with the server's public key as salt. |
| [`FromFileText(string)`](#m-fromfiletext-string) | Reads the text written by [`NetKeyPair.ToFileText()`](NetKeyPair.md#m-tofiletext). |
| [`FromPrivateKey(ReadOnlySpan<byte>)`](#m-fromprivatekey-readonlyspan-byte) | Rebuilds a key pair from its private key bytes. |
| [`Generate()`](#m-generate) | Makes a new random key pair from [`Entropy`](Entropy.md). |
| [`GenerateDevelopment()`](#m-generatedevelopment) | Makes a new random key pair marked [`NetKeyPair.IsDevelopment`](NetKeyPair.md#m-isdevelopment). |
| [`Load(string)`](#m-load-string) | Reads a key file written by [`NetKeyPair.Save()`](NetKeyPair.md#m-save-string). |
| [`Save(string)`](#m-save-string) | Writes the key file. |
| [`ToFileText()`](#m-tofiletext) | The key file text: comment lines, then `private:` and `public:` lines. |
| [`ToString()`](#m-tostring) | The key's text form is its public key; the private half never prints. |

### <a id="m-derivefor-peerkey"></a>DeriveFor(PeerKey)

```csharp
public NetKeyPair DeriveFor(PeerKey server);
```

A key pair derived from this one for one server: HKDF-SHA-256 of the private key with the server's public key as salt. A player who connects to many servers then shows each a different key, so hosts cannot recognise the same player by comparing keys, while each server still sees the same key every time that player connects to it.

**Parameters**

- `server` ([`PeerKey`](PeerKey.md)): The server's public key.

**Returns:** [`NetKeyPair`](NetKeyPair.md)

### <a id="m-fromfiletext-string"></a>FromFileText(string)

```csharp
public static NetKeyPair FromFileText(string text);
```

Reads the text written by [`NetKeyPair.ToFileText()`](NetKeyPair.md#m-tofiletext).

**Parameters**

- `text` (`string`): A key file's contents.

**Returns:** [`NetKeyPair`](NetKeyPair.md)

**Exceptions**

- [`KeyFormatException`](KeyFormatException.md): The text is not a key file, or its public line does not match its private line.

### <a id="m-fromprivatekey-readonlyspan-byte"></a>FromPrivateKey(ReadOnlySpan&lt;byte&gt;)

```csharp
public static NetKeyPair FromPrivateKey(ReadOnlySpan<byte> privateKey);
```

Rebuilds a key pair from its private key bytes.

**Parameters**

- `privateKey` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<byte>`): 32 bytes.

**Returns:** [`NetKeyPair`](NetKeyPair.md)

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The span is not 32 bytes.

### <a id="m-generate"></a>Generate()

```csharp
public static NetKeyPair Generate();
```

Makes a new random key pair from [`Entropy`](Entropy.md).

**Returns:** [`NetKeyPair`](NetKeyPair.md)

### <a id="m-generatedevelopment"></a>GenerateDevelopment()

```csharp
public static NetKeyPair GenerateDevelopment();
```

Makes a new random key pair marked [`NetKeyPair.IsDevelopment`](NetKeyPair.md#m-isdevelopment).

**Returns:** [`NetKeyPair`](NetKeyPair.md)

### <a id="m-load-string"></a>Load(string)

```csharp
public static NetKeyPair Load(string path);
```

Reads a key file written by [`NetKeyPair.Save()`](NetKeyPair.md#m-save-string).

**Parameters**

- `path` (`string`): The file.

**Returns:** [`NetKeyPair`](NetKeyPair.md)

**Exceptions**

- [`KeyFormatException`](KeyFormatException.md): The file is not a key file.

### <a id="m-save-string"></a>Save(string)

```csharp
public void Save(string path);
```

Writes the key file. Where the platform has file modes, the file is readable by its owner only.

**Parameters**

- `path` (`string`): The file to create or replace.

### <a id="m-tofiletext"></a>ToFileText()

```csharp
public string ToFileText();
```

The key file text: comment lines, then `private:` and `public:` lines. Treat it as a secret.

**Returns:** `string`

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The key's text form is its public key; the private half never prints.

**Returns:** `string`
