[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Resolution static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class Resolution
```

Named capture sizes.

## Methods

| Member | Description |
| --- | --- |
| [`Parse(string?)`](#m-parse-string) | Parses a size or throws. |
| [`TryParse(string?, out int, out int)`](#m-tryparse-string-int-int) | Parses a size such as `720p`, `1080p`, `4k`, `phone` or `1280x720`. |

### <a id="m-parse-string"></a>Parse(string?)

```csharp
public static (int Width, int Height) Parse(string? text);
```

Parses a size or throws.

**Parameters**

- `text` (`string?`)

**Returns:** `(int Width, int Height)`

**Exceptions**

- [`ArgumentException`](https://learn.microsoft.com/dotnet/api/system.argumentexception): The text is not a size.

### <a id="m-tryparse-string-int-int"></a>TryParse(string?, out int, out int)

```csharp
public static bool TryParse(string? text, out int width, out int height);
```

Parses a size such as `720p`, `1080p`, `4k`, `phone` or `1280x720`.

**Parameters**

- `text` (`string?`)
- `width` (`int`)
- `height` (`int`)

**Returns:** `bool`
