[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# StoreNames static class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public static class StoreNames
```

The rules for file names in an [`IByteStore`](IByteStore.md): ASCII letters, digits, `.`, `_` and `-`, up to 96 characters.

## Fields

| Member | Description |
| --- | --- |
| [`MaxLength`](#m-maxlength) | The longest name. |

### <a id="m-maxlength"></a>MaxLength

```csharp
public const int MaxLength = 96;
```

The longest name.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`IsValid(string?)`](#m-isvalid-string) | True when `name` is a valid file name: no path separators, no leading dot, not a temporary name. |
| [`Require(string?)`](#m-require-string) | Throws [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception) unless `name` is valid. |
| [`Sanitize(string)`](#m-sanitize-string) | Turns any text into a valid name part by replacing other characters with `_` (for a game's name in a folder). |

### <a id="m-isvalid-string"></a>IsValid(string?)

```csharp
public static bool IsValid(string? name);
```

True when `name` is a valid file name: no path separators, no leading dot, not a temporary name.

**Parameters**

- `name` (`string?`)

**Returns:** `bool`

### <a id="m-require-string"></a>Require(string?)

```csharp
public static void Require(string? name);
```

Throws [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception) unless `name` is valid.

**Parameters**

- `name` (`string?`)

### <a id="m-sanitize-string"></a>Sanitize(string)

```csharp
public static string Sanitize(string text);
```

Turns any text into a valid name part by replacing other characters with `_` (for a game's name in a folder).

**Parameters**

- `text` (`string`): Any non-empty text.

**Returns:** `string` — The text with each disallowed character replaced.
