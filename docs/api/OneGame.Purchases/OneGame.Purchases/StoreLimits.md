[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# StoreLimits static class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public static class StoreLimits
```

Limits on everything read from a store, so a hostile or broken store cannot make the game allocate without bound.

## Fields

| Member | Description |
| --- | --- |
| [`MaxItems`](#m-maxitems) | The most products or purchases one message may list. |
| [`MaxJsonDepth`](#m-maxjsondepth) | The deepest JSON nesting accepted. |
| [`MaxMessageBytes`](#m-maxmessagebytes) | The longest store message the bridge to a native store accepts, in bytes (256 KiB). |
| [`MaxProofChars`](#m-maxproofchars) | The longest proof (receipt, purchase token, signed data) kept, in UTF-16 characters. |
| [`MaxTextChars`](#m-maxtextchars) | The longest id, title or other short text kept, in UTF-16 characters. |

### <a id="m-maxitems"></a>MaxItems

```csharp
public const int MaxItems = 256;
```

The most products or purchases one message may list.

**Type:** `int`

### <a id="m-maxjsondepth"></a>MaxJsonDepth

```csharp
public const int MaxJsonDepth = 8;
```

The deepest JSON nesting accepted.

**Type:** `int`

### <a id="m-maxmessagebytes"></a>MaxMessageBytes

```csharp
public const int MaxMessageBytes = 262144;
```

The longest store message the bridge to a native store accepts, in bytes (256 KiB).

**Type:** `int`

### <a id="m-maxproofchars"></a>MaxProofChars

```csharp
public const int MaxProofChars = 1048576;
```

The longest proof (receipt, purchase token, signed data) kept, in UTF-16 characters.

**Type:** `int`

### <a id="m-maxtextchars"></a>MaxTextChars

```csharp
public const int MaxTextChars = 1024;
```

The longest id, title or other short text kept, in UTF-16 characters.

**Type:** `int`
