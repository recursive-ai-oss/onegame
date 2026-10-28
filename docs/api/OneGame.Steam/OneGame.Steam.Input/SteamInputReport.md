[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# SteamInputReport record

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`SteamInputReport`](SteamInputReport.md)&gt;

```csharp
public sealed record SteamInputReport : IEquatable<SteamInputReport>
```

What the harness can read about Steam Input.

## Constructors

| Member | Description |
| --- | --- |
| [`SteamInputReport(bool, string?, bool, string?, IReadOnlyList<SteamInputSetInfo>, IReadOnlyList<SteamControllerInfo>, IReadOnlyList<SteamActionValue>, SteamKeyboardInfo, IReadOnlyList<string>)`](#m-ctor-bool-string-bool-string-ireadonlylist-steaminputsetinfo-ireadonlylist-steamcontrollerinfo-ireadonlylist-steamactionvalue-steamkeyboardinfo-ireadonlylist-string) | What the harness can read about Steam Input. |

### <a id="m-ctor-bool-string-bool-string-ireadonlylist-steaminputsetinfo-ireadonlylist-steamcontrollerinfo-ireadonlylist-steamactionvalue-steamkeyboardinfo-ireadonlylist-string"></a>SteamInputReport(bool, string?, bool, string?, IReadOnlyList&lt;SteamInputSetInfo&gt;, IReadOnlyList&lt;SteamControllerInfo&gt;, IReadOnlyList&lt;SteamActionValue&gt;, SteamKeyboardInfo, IReadOnlyList&lt;string&gt;)

```csharp
public SteamInputReport(bool Enabled, string? Reason, bool Fake, string? ActiveSet, IReadOnlyList<SteamInputSetInfo> Sets, IReadOnlyList<SteamControllerInfo> Controllers, IReadOnlyList<SteamActionValue> Values, SteamKeyboardInfo Keyboard, IReadOnlyList<string> Missing);
```

What the harness can read about Steam Input.

**Parameters**

- `Enabled` (`bool`): True when Steam Input is running.
- `Reason` (`string?`): Why it is not, when it is not.
- `Fake` (`bool`): True for the harness stand-in that has no Steam behind it.
- `ActiveSet` (`string?`): The set the game last activated.
- `Sets` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamInputSetInfo`](SteamInputSetInfo.md)&gt;): The declared sets.
- `Controllers` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamControllerInfo`](SteamControllerInfo.md)&gt;): The connected controllers.
- `Values` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamActionValue`](SteamActionValue.md)&gt;): The latest value of every declared action, per controller.
- `Keyboard` ([`SteamKeyboardInfo`](SteamKeyboardInfo.md)): The on-screen keyboard's state.
- `Missing` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`): Declared actions Steam does not know (not in the actions file it loaded).

## Properties

| Member | Description |
| --- | --- |
| [`ActiveSet`](#m-activeset) | The set the game last activated. |
| [`Controllers`](#m-controllers) | The connected controllers. |
| [`Enabled`](#m-enabled) | True when Steam Input is running. |
| [`Fake`](#m-fake) | True for the harness stand-in that has no Steam behind it. |
| [`Keyboard`](#m-keyboard) | The on-screen keyboard's state. |
| [`Missing`](#m-missing) | Declared actions Steam does not know (not in the actions file it loaded). |
| [`Reason`](#m-reason) | Why it is not, when it is not. |
| [`Sets`](#m-sets) | The declared sets. |
| [`Values`](#m-values) | The latest value of every declared action, per controller. |

### <a id="m-activeset"></a>ActiveSet

```csharp
public string? ActiveSet { get; init; }
```

The set the game last activated.

**Type:** `string?`

### <a id="m-controllers"></a>Controllers

```csharp
public IReadOnlyList<SteamControllerInfo> Controllers { get; init; }
```

The connected controllers.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamControllerInfo`](SteamControllerInfo.md)&gt;

### <a id="m-enabled"></a>Enabled

```csharp
public bool Enabled { get; init; }
```

True when Steam Input is running.

**Type:** `bool`

### <a id="m-fake"></a>Fake

```csharp
public bool Fake { get; init; }
```

True for the harness stand-in that has no Steam behind it.

**Type:** `bool`

### <a id="m-keyboard"></a>Keyboard

```csharp
public SteamKeyboardInfo Keyboard { get; init; }
```

The on-screen keyboard's state.

**Type:** [`SteamKeyboardInfo`](SteamKeyboardInfo.md)

### <a id="m-missing"></a>Missing

```csharp
public IReadOnlyList<string> Missing { get; init; }
```

Declared actions Steam does not know (not in the actions file it loaded).

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

### <a id="m-reason"></a>Reason

```csharp
public string? Reason { get; init; }
```

Why it is not, when it is not.

**Type:** `string?`

### <a id="m-sets"></a>Sets

```csharp
public IReadOnlyList<SteamInputSetInfo> Sets { get; init; }
```

The declared sets.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamInputSetInfo`](SteamInputSetInfo.md)&gt;

### <a id="m-values"></a>Values

```csharp
public IReadOnlyList<SteamActionValue> Values { get; init; }
```

The latest value of every declared action, per controller.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`SteamActionValue`](SteamActionValue.md)&gt;
