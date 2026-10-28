[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# NativeWindowHandle struct

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`NativeWindowHandle`](NativeWindowHandle.md)&gt;

```csharp
public readonly struct NativeWindowHandle : IEquatable<NativeWindowHandle>
```

The raw handles a graphics API needs to create a presentable surface for a window.

## Constructors

| Member | Description |
| --- | --- |
| [`NativeWindowHandle(NativeWindowKind, nint, nint)`](#m-ctor-nativewindowkind-nint-nint) | The raw handles a graphics API needs to create a presentable surface for a window. |

### <a id="m-ctor-nativewindowkind-nint-nint"></a>NativeWindowHandle(NativeWindowKind, nint, nint)

```csharp
public NativeWindowHandle(NativeWindowKind Kind, nint Display, nint Window);
```

The raw handles a graphics API needs to create a presentable surface for a window.

**Parameters**

- `Kind` ([`NativeWindowKind`](NativeWindowKind.md)): Which window system the handles belong to.
- `Display` (`nint`): The display or instance handle; see [`NativeWindowKind`](NativeWindowKind.md).
- `Window` (`nint`): The window or surface handle; see [`NativeWindowKind`](NativeWindowKind.md).

## Properties

| Member | Description |
| --- | --- |
| [`Display`](#m-display) | The display or instance handle; see [`NativeWindowKind`](NativeWindowKind.md). |
| [`Kind`](#m-kind) | Which window system the handles belong to. |
| [`Window`](#m-window) | The window or surface handle; see [`NativeWindowKind`](NativeWindowKind.md). |

### <a id="m-display"></a>Display

```csharp
public nint Display { get; init; }
```

The display or instance handle; see [`NativeWindowKind`](NativeWindowKind.md).

**Type:** `nint`

### <a id="m-kind"></a>Kind

```csharp
public NativeWindowKind Kind { get; init; }
```

Which window system the handles belong to.

**Type:** [`NativeWindowKind`](NativeWindowKind.md)

### <a id="m-window"></a>Window

```csharp
public nint Window { get; init; }
```

The window or surface handle; see [`NativeWindowKind`](NativeWindowKind.md).

**Type:** `nint`
