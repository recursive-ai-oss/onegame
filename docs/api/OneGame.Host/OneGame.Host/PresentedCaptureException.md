[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# PresentedCaptureException class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)  
**Inherits:** [`InvalidOperationException`](https://learn.microsoft.com/dotnet/api/system.invalidoperationexception)

```csharp
public sealed class PresentedCaptureException : InvalidOperationException
```

A presented capture was asked for from a game that has a window and a graphics device, but no frame could be drawn and read back.

## Constructors

| Member | Description |
| --- | --- |
| [`PresentedCaptureException(PresentedCaptureFailure)`](#m-ctor-presentedcapturefailure) | Creates the exception for a reason. |

### <a id="m-ctor-presentedcapturefailure"></a>PresentedCaptureException(PresentedCaptureFailure)

```csharp
public PresentedCaptureException(PresentedCaptureFailure reason);
```

Creates the exception for a reason.

**Parameters**

- `reason` ([`PresentedCaptureFailure`](PresentedCaptureFailure.md)): Why no frame was presented.

## Properties

| Member | Description |
| --- | --- |
| [`Reason`](#m-reason) | Why no frame was presented. |

### <a id="m-reason"></a>Reason

```csharp
public PresentedCaptureFailure Reason { get; }
```

Why no frame was presented.

**Type:** [`PresentedCaptureFailure`](PresentedCaptureFailure.md)
