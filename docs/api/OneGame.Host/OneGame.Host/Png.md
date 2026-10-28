[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# Png static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class Png
```

PNG reading and writing for captures, through OneImage.

## Fields

| Member | Description |
| --- | --- |
| [`MaxSide`](#m-maxside) | Largest image the reader accepts, in pixels per side. |

### <a id="m-maxside"></a>MaxSide

```csharp
public const int MaxSide = 16384;
```

Largest image the reader accepts, in pixels per side.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Decode(byte[], string)`](#m-decode-byte-string) | Decodes a PNG (or any format OneImage reads) into RGBA8. |
| [`Encode(CapturedFrame)`](#m-encode-capturedframe) | Encodes a frame as PNG bytes. |
| [`Load(string)`](#m-load-string) | Reads an image file. |
| [`Save(CapturedFrame, string)`](#m-save-capturedframe-string) | Writes a frame to a PNG file, creating the folder. |

### <a id="m-decode-byte-string"></a>Decode(byte\[\], string)

```csharp
public static CapturedFrame Decode(byte[] bytes, string renderer = "file");
```

Decodes a PNG (or any format OneImage reads) into RGBA8. The size is checked before pixels are allocated.

**Parameters**

- `bytes` (`byte[]`)
- `renderer` (`string`)

**Returns:** [`CapturedFrame`](CapturedFrame.md)

**Exceptions**

- [`InvalidDataException`](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception): The bytes are not a readable image, or the image is larger than [`Png.MaxSide`](Png.md#m-maxside).

### <a id="m-encode-capturedframe"></a>Encode(CapturedFrame)

```csharp
public static byte[] Encode(CapturedFrame frame);
```

Encodes a frame as PNG bytes.

**Parameters**

- `frame` ([`CapturedFrame`](CapturedFrame.md))

**Returns:** `byte[]`

### <a id="m-load-string"></a>Load(string)

```csharp
public static CapturedFrame Load(string path);
```

Reads an image file.

**Parameters**

- `path` (`string`)

**Returns:** [`CapturedFrame`](CapturedFrame.md)

### <a id="m-save-capturedframe-string"></a>Save(CapturedFrame, string)

```csharp
public static void Save(CapturedFrame frame, string path);
```

Writes a frame to a PNG file, creating the folder.

**Parameters**

- `frame` ([`CapturedFrame`](CapturedFrame.md))
- `path` (`string`)
