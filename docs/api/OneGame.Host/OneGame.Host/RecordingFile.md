[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# RecordingFile static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class RecordingFile
```

Reads and writes recording files. A recording file is untrusted input when read.

## Methods

| Member | Description |
| --- | --- |
| [`Load(string)`](#m-load-string) | Reads a recording file. |
| [`LoadSession(string)`](#m-loadsession-string) | Reads a session file: either a session container or a plain single recording (one segment). |
| [`Save(SessionRecording, string)`](#m-save-sessionrecording-string) | Writes a session, creating the folder when needed. |
| [`Save(Recording, string)`](#m-save-recording-string) | Writes a recording, creating the folder when needed. |

### <a id="m-load-string"></a>Load(string)

```csharp
public static Recording Load(string path);
```

Reads a recording file.

**Parameters**

- `path` (`string`)

**Returns:** [`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md)

**Exceptions**

- [`FileNotFoundException`](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception): There is no such file.
- [`InvalidDataException`](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception): The file is too large, or is not a recording this build reads.

### <a id="m-loadsession-string"></a>LoadSession(string)

```csharp
public static SessionRecording LoadSession(string path);
```

Reads a session file: either a session container or a plain single recording (one segment).

**Parameters**

- `path` (`string`)

**Returns:** [`SessionRecording`](SessionRecording.md)

**Exceptions**

- [`FileNotFoundException`](https://learn.microsoft.com/dotnet/api/system.io.filenotfoundexception): There is no such file.
- [`InvalidDataException`](https://learn.microsoft.com/dotnet/api/system.io.invaliddataexception): The file is too large, or is not a recording this build reads.

### <a id="m-save-sessionrecording-string"></a>Save(SessionRecording, string)

```csharp
public static void Save(SessionRecording session, string path);
```

Writes a session, creating the folder when needed.

**Parameters**

- `session` ([`SessionRecording`](SessionRecording.md))
- `path` (`string`)

### <a id="m-save-recording-string"></a>Save(Recording, string)

```csharp
public static void Save(Recording recording, string path);
```

Writes a recording, creating the folder when needed.

**Parameters**

- `recording` ([`Recording`](../../OneGame.Sync/OneGame.Sync/Recording.md))
- `path` (`string`)
