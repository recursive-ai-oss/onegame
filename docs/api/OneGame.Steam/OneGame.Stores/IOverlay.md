[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IOverlay interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IOverlay
```

The store's in-game overlay. Games usually pause when it opens.

## Properties

| Member | Description |
| --- | --- |
| [`IsActive`](#m-isactive) | True while the overlay is on screen. |
| [`IsEnabled`](#m-isenabled) | True when the overlay can be shown (the player has it enabled and it hooked the renderer). |

### <a id="m-isactive"></a>IsActive

```csharp
bool IsActive { get; }
```

True while the overlay is on screen. Updated from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump).

**Type:** `bool`

### <a id="m-isenabled"></a>IsEnabled

```csharp
bool IsEnabled { get; }
```

True when the overlay can be shown (the player has it enabled and it hooked the renderer).

**Type:** `bool`

## Events

| Member | Description |
| --- | --- |
| [`ActiveChanged`](#m-activechanged) | Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) when the overlay opens (true) or closes (false). |

### <a id="m-activechanged"></a>ActiveChanged

```csharp
event Action<bool>? ActiveChanged;
```

Raised from [`IStorePlatform.Pump()`](IStorePlatform.md#m-pump) when the overlay opens (true) or closes (false).

**Type:** [`Action`](https://learn.microsoft.com/dotnet/api/system.action-1)`<bool>?`

## Methods

| Member | Description |
| --- | --- |
| [`Open(OverlayPage)`](#m-open-overlaypage) | Opens an overlay page. |
| [`OpenStorePage(uint)`](#m-openstorepage-uint) | Opens a store page for an app in the overlay. |
| [`OpenWebPage(string)`](#m-openwebpage-string) | Opens a web page in the overlay browser. |

### <a id="m-open-overlaypage"></a>Open(OverlayPage)

```csharp
StoreResult Open(OverlayPage page);
```

Opens an overlay page.

**Parameters**

- `page` ([`OverlayPage`](OverlayPage.md))

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-openstorepage-uint"></a>OpenStorePage(uint)

```csharp
StoreResult OpenStorePage(uint appId);
```

Opens a store page for an app in the overlay.

**Parameters**

- `appId` (`uint`)

**Returns:** [`StoreResult`](StoreResult.md)

### <a id="m-openwebpage-string"></a>OpenWebPage(string)

```csharp
StoreResult OpenWebPage(string url);
```

Opens a web page in the overlay browser. Only http and https URLs are accepted.

**Parameters**

- `url` (`string`)

**Returns:** [`StoreResult`](StoreResult.md)
