[API reference](../../index.md) › [OneGame.Server](../index.md) › OneGame.Server

# SecurityConfig record

**Namespace:** `OneGame.Server`  
**Package:** [`RecursiveAI.OneGame.Server`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SecurityConfig`](SecurityConfig.md)&gt;

```csharp
public sealed record SecurityConfig : IEquatable<SecurityConfig>
```

How the server secures its client links (the `security:` section).

## Constructors

| Member | Description |
| --- | --- |
| [`SecurityConfig(IReadOnlyList<string>, string, bool)`](#m-ctor-ireadonlylist-string-string-bool) | How the server secures its client links (the `security:` section). |

### <a id="m-ctor-ireadonlylist-string-string-bool"></a>SecurityConfig(IReadOnlyList&lt;string&gt;, string, bool)

```csharp
public SecurityConfig(IReadOnlyList<string> KeyFiles, string GameId, bool Insecure);
```

How the server secures its client links (the `security:` section).

**Parameters**

- `KeyFiles` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): The server's key files (made by `onegame keys`), primary first; more than one lets the host rotate keys. Relative paths resolve against the configuration file.
- `GameId` (`string`): The game and protocol name clients must use too; any text, empty by default.
- `Insecure` (`bool`): True to listen without encryption or authentication (tests and local tools only).

## Properties

| Member | Description |
| --- | --- |
| [`GameId`](#m-gameid) | The game and protocol name clients must use too; any text, empty by default. |
| [`Insecure`](#m-insecure) | True to listen without encryption or authentication (tests and local tools only). |
| [`KeyFiles`](#m-keyfiles) | The server's key files (made by `onegame keys`), primary first; more than one lets the host rotate keys. |

### <a id="m-gameid"></a>GameId

```csharp
public string GameId { get; init; }
```

The game and protocol name clients must use too; any text, empty by default.

**Type:** `string`

### <a id="m-insecure"></a>Insecure

```csharp
public bool Insecure { get; init; }
```

True to listen without encryption or authentication (tests and local tools only).

**Type:** `bool`

### <a id="m-keyfiles"></a>KeyFiles

```csharp
public IReadOnlyList<string> KeyFiles { get; init; }
```

The server's key files (made by `onegame keys`), primary first; more than one lets the host rotate keys. Relative paths resolve against the configuration file.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`
