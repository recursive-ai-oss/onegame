[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IOwnership interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IOwnership
```

Ownership of the app and its DLC.

## Properties

| Member | Description |
| --- | --- |
| [`AppId`](#m-appid) | The store's id for this app; 0 when unknown. |
| [`BuildId`](#m-buildid) | The store's build number of the installed version; 0 when unknown. |
| [`OwnsApp`](#m-ownsapp) | True when the signed-in player owns this app (or is allowed to play it through family sharing or a trial). |

### <a id="m-appid"></a>AppId

```csharp
uint AppId { get; }
```

The store's id for this app; 0 when unknown.

**Type:** `uint`

### <a id="m-buildid"></a>BuildId

```csharp
int BuildId { get; }
```

The store's build number of the installed version; 0 when unknown.

**Type:** `int`

### <a id="m-ownsapp"></a>OwnsApp

```csharp
bool OwnsApp { get; }
```

True when the signed-in player owns this app (or is allowed to play it through family sharing or a trial).

**Type:** `bool`

## Methods

| Member | Description |
| --- | --- |
| [`HasDlc(uint)`](#m-hasdlc-uint) | True when the player owns a DLC and has it installed. |

### <a id="m-hasdlc-uint"></a>HasDlc(uint)

```csharp
bool HasDlc(uint dlcAppId);
```

True when the player owns a DLC and has it installed.

**Parameters**

- `dlcAppId` (`uint`)

**Returns:** `bool`
