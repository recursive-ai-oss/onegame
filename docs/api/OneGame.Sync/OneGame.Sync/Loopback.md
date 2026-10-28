[API reference](../../index.md) › [OneGame.Sync](../index.md) › OneGame.Sync

# Loopback static class

**Namespace:** `OneGame.Sync`  
**Package:** [`RecursiveAI.OneGame.Sync`](../index.md)

```csharp
public static class Loopback
```

In-process transport: two channel ends joined by byte queues that frame messages with a length prefix, so receivers see exactly the bytes a socket would deliver.

## Fields

| Member | Description |
| --- | --- |
| [`MaxMessageBytes`](#m-maxmessagebytes) | Largest message a channel carries. |

### <a id="m-maxmessagebytes"></a>MaxMessageBytes

```csharp
public const int MaxMessageBytes = 268435456;
```

Largest message a channel carries.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`CreatePair()`](#m-createpair) | Creates two connected ends: what one sends, the other receives. |

### <a id="m-createpair"></a>CreatePair()

```csharp
public static (IMessageChannel A, IMessageChannel B) CreatePair();
```

Creates two connected ends: what one sends, the other receives.

**Returns:** ([`IMessageChannel`](IMessageChannel.md) `A,` [`IMessageChannel`](IMessageChannel.md) `B)`
