[API reference](../../index.md) › [OneGame.Server.Live](../index.md) › OneGame.Server.Live

# AudienceWatched record

**Namespace:** `OneGame.Server.Live`  
**Package:** [`RecursiveAI.OneGame.Server.Live`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`AudienceWatched`](AudienceWatched.md)&gt;

```csharp
public sealed record AudienceWatched : IEquatable<AudienceWatched>
```

Result of `audience_watch`.

## Constructors

| Member | Description |
| --- | --- |
| [`AudienceWatched(string, int)`](#m-ctor-string-int) | Result of `audience_watch`. |

### <a id="m-ctor-string-int"></a>AudienceWatched(string, int)

```csharp
public AudienceWatched(string Key, int Watching);
```

Result of `audience_watch`.

**Parameters**

- `Key` (`string`): The audience's key; the host records its view hash of every scope at every tick from now on.
- `Watching` (`int`): How many audiences the host records.

## Properties

| Member | Description |
| --- | --- |
| [`Key`](#m-key) | The audience's key; the host records its view hash of every scope at every tick from now on. |
| [`Watching`](#m-watching) | How many audiences the host records. |

### <a id="m-key"></a>Key

```csharp
public string Key { get; init; }
```

The audience's key; the host records its view hash of every scope at every tick from now on.

**Type:** `string`

### <a id="m-watching"></a>Watching

```csharp
public int Watching { get; init; }
```

How many audiences the host records.

**Type:** `int`
