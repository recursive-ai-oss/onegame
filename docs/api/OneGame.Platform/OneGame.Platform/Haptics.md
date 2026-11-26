[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# Haptics static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class Haptics
```

Touch feedback for view and host code. Call [`Haptics.Tick()`](Haptics.md#m-tick) when a piece snaps and [`Haptics.Buzz()`](Haptics.md#m-buzz) for a bigger moment.

Android plays the request through `View.performHapticFeedback` on the game's window, which needs no permission and follows the user's "touch feedback" setting by itself. iOS uses the feedback generators (`UIImpactFeedbackGenerator`, `UISelectionFeedbackGenerator`, `UINotificationFeedbackGenerator`), which the system silences when haptics are off. The simulator has no haptic engine, so the call runs and nothing is felt. Desktop and the web do nothing; a gamepad's rumble is [`GamepadManager.Rumble()`](GamepadManager.md#m-rumble-int-float-float-int).

Every call is recorded on every platform, so a scenario can assert on [`Haptics.GetLog()`](Haptics.md#m-getlog) (the `haptic:` expectation and the `haptics` control operation read it). Call it from views, input handlers and the host, never from sim code: the sim runs during replays and on servers, where nobody holds a phone.

## Properties

| Member | Description |
| --- | --- |
| [`Enabled`](#m-enabled) | Whether requests reach the device. |
| [`FrameClock`](#m-frameclock) | Tells the log which game frame it is, for the calling thread. |

### <a id="m-enabled"></a>Enabled

```csharp
public static bool Enabled { get; set; }
```

Whether requests reach the device. A game's "vibration" setting sets this; requests made while it is off are still logged, as not delivered.

**Type:** `bool`

### <a id="m-frameclock"></a>FrameClock

```csharp
public static Func<long>? FrameClock { get; set; }
```

Tells the log which game frame it is, for the calling thread. The game instance binds it to its frame counter on the thread that runs its frames (so two games in one process, as in tests, each stamp their own); a game never needs to. Requests made on a thread with no clock are stamped -1.

**Type:** [`Func`](https://learn.microsoft.com/dotnet/api/system.func-1)`<long>?`

## Methods

| Member | Description |
| --- | --- |
| [`Buzz()`](#m-buzz) | A strong pulse. |
| [`ClearLog()`](#m-clearlog) | Empties the log. |
| [`GetLog()`](#m-getlog) | The requests so far, oldest first, up to the last 1024. |
| [`Play(HapticKind)`](#m-play-haptickind) | Plays one kind of feedback. |
| [`Tick()`](#m-tick) | A very light tick. |

### <a id="m-buzz"></a>Buzz()

```csharp
public static void Buzz();
```

A strong pulse.

### <a id="m-clearlog"></a>ClearLog()

```csharp
public static void ClearLog();
```

Empties the log. Sequence numbers keep counting.

### <a id="m-getlog"></a>GetLog()

```csharp
public static IReadOnlyList<HapticEvent> GetLog();
```

The requests so far, oldest first, up to the last 1024. Works on every platform.

**Returns:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`HapticEvent`](HapticEvent.md)&gt; — A copy of the log.

### <a id="m-play-haptickind"></a>Play(HapticKind)

```csharp
public static void Play(HapticKind kind);
```

Plays one kind of feedback.

**Parameters**

- `kind` ([`HapticKind`](HapticKind.md)): The effect to play.

**Exceptions**

- [`ArgumentOutOfRangeException`](https://learn.microsoft.com/dotnet/api/system.argumentoutofrangeexception): `kind` is not a defined [`HapticKind`](HapticKind.md).

### <a id="m-tick"></a>Tick()

```csharp
public static void Tick();
```

A very light tick.
