[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# ISimEvent&lt;TSelf&gt; interface

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Implements:** [`ISyncValue`](ISyncValue-1.md)`<TSelf>`, [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)`<TSelf>`

```csharp
public interface ISimEvent<TSelf> : ISyncValue<TSelf>, IEquatable<TSelf> where TSelf : ISimEvent<TSelf>
```

A struct the generator made from [`SimEventAttribute`](SimEventAttribute.md): a synced value with a stable type id.

**Type parameters**

- `TSelf`: The event struct.

## Properties

| Member | Description |
| --- | --- |
| [`EventName`](#m-eventname) | The event's name in harness output and scenarios (the type name). |
| [`EventTypeId`](#m-eventtypeid) | The event's id on the wire: a hash of its full type name. |
| [`EventVisibility`](#m-eventvisibility) | Who may receive the event. |

### <a id="m-eventname"></a>EventName

```csharp
static abstract string EventName { get; }
```

The event's name in harness output and scenarios (the type name).

**Type:** `string`

### <a id="m-eventtypeid"></a>EventTypeId

```csharp
static abstract uint EventTypeId { get; }
```

The event's id on the wire: a hash of its full type name.

**Type:** `uint`

### <a id="m-eventvisibility"></a>EventVisibility

```csharp
static abstract Visibility EventVisibility { get; }
```

Who may receive the event.

**Type:** [`Visibility`](Visibility.md)
