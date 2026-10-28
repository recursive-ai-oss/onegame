[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# PlatformOptions record

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`PlatformOptions`](PlatformOptions.md)&gt;

```csharp
public sealed record PlatformOptions : IEquatable<PlatformOptions>
```

Options for [`PlatformApp.Create()`](PlatformApp.md#m-create-platformoptions).

## Constructors

| Member | Description |
| --- | --- |
| [`PlatformOptions()`](#m-ctor) |  |

### <a id="m-ctor"></a>PlatformOptions()

```csharp
public PlatformOptions();
```

## Properties

| Member | Description |
| --- | --- |
| [`Backend`](#m-backend) | The backend to use. |
| [`DisplayName`](#m-displayname) | The display to connect to: an X display name such as `:1`, or a Wayland socket name such as `wayland-1`. |
| [`EnableGamepadDevices`](#m-enablegamepaddevices) | Whether to look for real gamepad devices. |
| [`StickDeadZone`](#m-stickdeadzone) | Radial dead zone applied to gamepad sticks, as a fraction of full travel. |
| [`TouchEmulatesPointer`](#m-touchemulatespointer) | Whether the first finger on the screen also drives the pointer (move, left button down and up), so games written for a mouse work with a finger. |
| [`TriggerDeadZone`](#m-triggerdeadzone) | Dead zone applied to gamepad triggers, as a fraction of full travel. |

### <a id="m-backend"></a>Backend

```csharp
public PlatformBackendKind Backend { get; init; }
```

The backend to use. [`PlatformBackendKind.Auto`](PlatformBackendKind.md#m-auto) falls back to headless when no display is reachable.

**Type:** [`PlatformBackendKind`](PlatformBackendKind.md)

### <a id="m-displayname"></a>DisplayName

```csharp
public string? DisplayName { get; init; }
```

The display to connect to: an X display name such as `:1`, or a Wayland socket name such as `wayland-1`. When null the DISPLAY and WAYLAND\_DISPLAY environment variables of the process decide.

**Type:** `string?`

### <a id="m-enablegamepaddevices"></a>EnableGamepadDevices

```csharp
public bool EnableGamepadDevices { get; init; }
```

Whether to look for real gamepad devices. Tests that only want injected pads turn this off.

**Type:** `bool`

### <a id="m-stickdeadzone"></a>StickDeadZone

```csharp
public float StickDeadZone { get; init; }
```

Radial dead zone applied to gamepad sticks, as a fraction of full travel.

**Type:** `float`

### <a id="m-touchemulatespointer"></a>TouchEmulatesPointer

```csharp
public bool TouchEmulatesPointer { get; init; }
```

Whether the first finger on the screen also drives the pointer (move, left button down and up), so games written for a mouse work with a finger. Touch events are posted either way. Turn it off for a game that handles touch itself.

**Type:** `bool`

### <a id="m-triggerdeadzone"></a>TriggerDeadZone

```csharp
public float TriggerDeadZone { get; init; }
```

Dead zone applied to gamepad triggers, as a fraction of full travel.

**Type:** `float`
