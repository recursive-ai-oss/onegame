[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# LoadedFont record

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`LoadedFont`](LoadedFont.md)&gt;

```csharp
public sealed record LoadedFont : IEquatable<LoadedFont>
```

A font added to a library by [`Assets.LoadFont()`](Assets.md#m-loadfont-string-fontlibrary).

## Constructors

| Member | Description |
| --- | --- |
| [`LoadedFont(FontLibrary, int)`](#m-ctor-fontlibrary-int) | A font added to a library by [`Assets.LoadFont()`](Assets.md#m-loadfont-string-fontlibrary). |

### <a id="m-ctor-fontlibrary-int"></a>LoadedFont(FontLibrary, int)

```csharp
public LoadedFont(FontLibrary Library, int FaceId);
```

A font added to a library by [`Assets.LoadFont()`](Assets.md#m-loadfont-string-fontlibrary).

**Parameters**

- `Library` ([`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)): The library the face lives in.
- `FaceId` (`int`): The face id to use in glyph keys and font stacks.

## Properties

| Member | Description |
| --- | --- |
| [`FaceId`](#m-faceid) | The face id to use in glyph keys and font stacks. |
| [`Library`](#m-library) | The library the face lives in. |

### <a id="m-faceid"></a>FaceId

```csharp
public int FaceId { get; init; }
```

The face id to use in glyph keys and font stacks.

**Type:** `int`

### <a id="m-library"></a>Library

```csharp
public FontLibrary Library { get; init; }
```

The library the face lives in.

**Type:** [`FontLibrary`](../../OneGame.Text/OneGame.Text/FontLibrary.md)
