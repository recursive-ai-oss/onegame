[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ExitCodes static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class ExitCodes
```

Process exit codes shared by games and the `onegame` command.

## Fields

| Member | Description |
| --- | --- |
| [`BuildFailed`](#m-buildfailed) | The build failed. |
| [`Failed`](#m-failed) | The run completed but a check failed (a scenario expectation, a replay mismatch, a test). |
| [`NoDevTools`](#m-nodevtools) | The requested feature exists only in development builds and this build has none of it. |
| [`Ok`](#m-ok) | Success. |
| [`RuntimeError`](#m-runtimeerror) | The game or tool crashed or could not start (no display, no project, unreadable file). |
| [`Usage`](#m-usage) | Bad command line. |

### <a id="m-buildfailed"></a>BuildFailed

```csharp
public const int BuildFailed = 3;
```

The build failed.

**Type:** `int`

### <a id="m-failed"></a>Failed

```csharp
public const int Failed = 1;
```

The run completed but a check failed (a scenario expectation, a replay mismatch, a test).

**Type:** `int`

### <a id="m-nodevtools"></a>NoDevTools

```csharp
public const int NoDevTools = 5;
```

The requested feature exists only in development builds and this build has none of it.

**Type:** `int`

### <a id="m-ok"></a>Ok

```csharp
public const int Ok = 0;
```

Success.

**Type:** `int`

### <a id="m-runtimeerror"></a>RuntimeError

```csharp
public const int RuntimeError = 4;
```

The game or tool crashed or could not start (no display, no project, unreadable file).

**Type:** `int`

### <a id="m-usage"></a>Usage

```csharp
public const int Usage = 2;
```

Bad command line.

**Type:** `int`
