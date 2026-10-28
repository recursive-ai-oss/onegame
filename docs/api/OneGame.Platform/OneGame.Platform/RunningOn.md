[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# RunningOn static class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public static class RunningOn
```

Which kind of system the game is running on. Use this instead of [`OperatingSystem`](https://learn.microsoft.com/dotnet/api/system.operatingsystem) for Android: a NativeAOT game built for Android (`linux-bionic`) runs on the Linux runtime, so `OperatingSystem.IsAndroid()` is false there while `OperatingSystem.IsLinux()` is true. The answers are fixed for the life of the process and are right from the first call.

## Properties

| Member | Description |
| --- | --- |
| [`Android`](#m-android) | True in an Android app, whether the game runs on the Android runtime or as a NativeAOT library. |
| [`Browser`](#m-browser) | True in a web page. |
| [`Desktop`](#m-desktop) | True on Windows, macOS or Linux that is not Android. |
| [`IOS`](#m-ios) | True on iOS (not macOS). |
| [`Mobile`](#m-mobile) | True on Android, iOS or in a browser: where a game has no process of its own to quit. |
| [`Phone`](#m-phone) | True on Android or iOS: a touch device with no program to quit and no window to close. |

### <a id="m-android"></a>Android

```csharp
public static bool Android { get; }
```

True in an Android app, whether the game runs on the Android runtime or as a NativeAOT library.

**Type:** `bool`

### <a id="m-browser"></a>Browser

```csharp
public static bool Browser { get; }
```

True in a web page.

**Type:** `bool`

### <a id="m-desktop"></a>Desktop

```csharp
public static bool Desktop { get; }
```

True on Windows, macOS or Linux that is not Android.

**Type:** `bool`

### <a id="m-ios"></a>IOS

```csharp
public static bool IOS { get; }
```

True on iOS (not macOS).

**Type:** `bool`

### <a id="m-mobile"></a>Mobile

```csharp
public static bool Mobile { get; }
```

True on Android, iOS or in a browser: where a game has no process of its own to quit.

**Type:** `bool`

### <a id="m-phone"></a>Phone

```csharp
public static bool Phone { get; }
```

True on Android or iOS: a touch device with no program to quit and no window to close.

**Type:** `bool`
