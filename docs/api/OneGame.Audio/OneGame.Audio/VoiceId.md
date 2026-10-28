[API reference](../../index.md) › [OneGame.Audio](../index.md) › OneGame.Audio

# VoiceId struct

**Namespace:** `OneGame.Audio`  
**Package:** [`RecursiveAI.OneGame.Audio`](../index.md)  
**Implements:** [`IEquatable`](https://learn.microsoft.com/dotnet/api/system.iequatable-1)&lt;[`VoiceId`](VoiceId.md)&gt;

```csharp
public readonly struct VoiceId : IEquatable<VoiceId>
```

Identifies one playing sound inside a core. Ids are never reused, so a stale id is simply ignored.

## Constructors

| Member | Description |
| --- | --- |
| [`VoiceId(long)`](#m-ctor-long) | Identifies one playing sound inside a core. |

### <a id="m-ctor-long"></a>VoiceId(long)

```csharp
public VoiceId(long Id);
```

Identifies one playing sound inside a core. Ids are never reused, so a stale id is simply ignored.

**Parameters**

- `Id` (`long`): The id; 0 or less is invalid.

## Properties

| Member | Description |
| --- | --- |
| [`Id`](#m-id) | The id; 0 or less is invalid. |
| [`IsValid`](#m-isvalid) | Whether the id refers to a voice that was accepted for playing. |

### <a id="m-id"></a>Id

```csharp
public long Id { get; init; }
```

The id; 0 or less is invalid.

**Type:** `long`

### <a id="m-isvalid"></a>IsValid

```csharp
public bool IsValid { get; }
```

Whether the id refers to a voice that was accepted for playing.

**Type:** `bool`
