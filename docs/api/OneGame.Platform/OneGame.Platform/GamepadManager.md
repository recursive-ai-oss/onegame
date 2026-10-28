[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# GamepadManager class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IDisposable`](https://learn.microsoft.com/dotnet/api/system.idisposable)

```csharp
public sealed class GamepadManager : IDisposable
```

Finds gamepads, maps them onto the standard layout, and plays rumble. Created by [`PlatformApp`](PlatformApp.md).

## Properties

| Member | Description |
| --- | --- |
| [`Mappings`](#m-mappings) | Mappings used to find a device's layout. |

### <a id="m-mappings"></a>Mappings

```csharp
public GamepadMappingDatabase Mappings { get; }
```

Mappings used to find a device's layout. Devices with no entry get the kernel's standard layout on Linux. Add database text here before pads are plugged in, or call [`GamepadManager.Rescan()`](GamepadManager.md#m-rescan) after.

**Type:** [`GamepadMappingDatabase`](GamepadMappingDatabase.md)

## Methods

| Member | Description |
| --- | --- |
| [`Dispose()`](#m-dispose) |  |
| [`Rescan()`](#m-rescan) | Forgets devices that failed to open and looks for gamepads again on the next [`PlatformApp.PumpEvents()`](PlatformApp.md#m-pumpevents-int). |
| [`Rumble(int, float, float, int)`](#m-rumble-int-float-float-int) | Plays rumble on a gamepad for a while. |

### <a id="m-dispose"></a>Dispose()

```csharp
public void Dispose();
```

Implements [`IDisposable.Dispose()`](https://learn.microsoft.com/dotnet/api/system.idisposable.dispose).

### <a id="m-rescan"></a>Rescan()

```csharp
public void Rescan();
```

Forgets devices that failed to open and looks for gamepads again on the next [`PlatformApp.PumpEvents()`](PlatformApp.md#m-pumpevents-int).

### <a id="m-rumble-int-float-float-int"></a>Rumble(int, float, float, int)

```csharp
public bool Rumble(int gamepadId, float low, float high, int durationMilliseconds);
```

Plays rumble on a gamepad for a while. Strengths run from 0 to 1: `low` is the heavy low-frequency motor, `high` the light one. Returns false when the device cannot rumble.

**Parameters**

- `gamepadId` (`int`)
- `low` (`float`)
- `high` (`float`)
- `durationMilliseconds` (`int`)

**Returns:** `bool`
