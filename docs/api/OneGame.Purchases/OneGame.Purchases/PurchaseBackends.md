[API reference](../../index.md) › [OneGame.Purchases](../index.md) › OneGame.Purchases

# PurchaseBackends static class

**Namespace:** `OneGame.Purchases`  
**Package:** [`RecursiveAI.OneGame.Purchases`](../index.md)

```csharp
public static class PurchaseBackends
```

Chooses the purchase backend for the running platform.

## Fields

| Member | Description |
| --- | --- |
| [`BackendVariable`](#m-backendvariable) | Set to `fake` to use a [`FakeStore`](FakeStore.md) everywhere (a development run or a test), whatever the platform. |

### <a id="m-backendvariable"></a>BackendVariable

```csharp
public const string BackendVariable = "ONEGAME_STORE";
```

Set to `fake` to use a [`FakeStore`](FakeStore.md) everywhere (a development run or a test), whatever the platform.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`CreateNative()`](#m-createnative) | The platform's real store: Google Play on Android, the App Store on iOS and macOS. |
| [`CreateService(IReadOnlyList<ProductDefinition>)`](#m-createservice-ireadonlylist-productdefinition) | Makes a service for a game: the fake store when [`PurchaseBackends.BackendVariable`](PurchaseBackends.md#m-backendvariable) is `fake`, otherwise [`PurchaseBackends.CreateNative()`](PurchaseBackends.md#m-createnative). |

### <a id="m-createnative"></a>CreateNative()

```csharp
public static IPurchaseBackend CreateNative();
```

The platform's real store: Google Play on Android, the App Store on iOS and macOS. Anywhere else, or when the store cannot be set up, an [`UnavailableStore`](UnavailableStore.md) that says why.

**Returns:** [`IPurchaseBackend`](IPurchaseBackend.md)

### <a id="m-createservice-ireadonlylist-productdefinition"></a>CreateService(IReadOnlyList&lt;ProductDefinition&gt;)

```csharp
public static PurchaseService CreateService(IReadOnlyList<ProductDefinition> catalog);
```

Makes a service for a game: the fake store when [`PurchaseBackends.BackendVariable`](PurchaseBackends.md#m-backendvariable) is `fake`, otherwise [`PurchaseBackends.CreateNative()`](PurchaseBackends.md#m-createnative). The fake sells every product in the catalog.

**Parameters**

- `catalog` ([`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`ProductDefinition`](ProductDefinition.md)&gt;)

**Returns:** [`PurchaseService`](PurchaseService.md)
