[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# CommandAttribute attribute

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)  
**Inherits:** [`Attribute`](https://learn.microsoft.com/dotnet/api/system.attribute)  
**Applies to:** `Method`

```csharp
public sealed class CommandAttribute : Attribute
```

Marks an entity method as a command: a request that a view or test sends and the authority validates and runs. Call it through `entity.Send.Name(...)`; calling the method directly runs it here and now, which only sim code should do.

## Constructors

| Member | Description |
| --- | --- |
| [`CommandAttribute()`](#m-ctor) |  |

### <a id="m-ctor"></a>CommandAttribute()

```csharp
public CommandAttribute();
```

## Properties

| Member | Description |
| --- | --- |
| [`OwnerOnly`](#m-owneronly) | When true, the authority accepts the command from a connection only if its trust owns the scope (`Trust.Own`) and its player is the target entity's owner ([`OwnerKeyAttribute`](OwnerKeyAttribute.md)). |
| [`Predicted`](#m-predicted) | When true, a replica that has prediction enabled runs this command on its own predicted copy of the scope the moment it is sent, and again (rolled back and resimulated) whenever the authority's state arrives, until the authority has answered. |
| [`UsesRandom`](#m-usesrandom) | Says that a [`CommandAttribute.Predicted`](CommandAttribute.md#m-predicted) handler draws from a named random stream. |

### <a id="m-owneronly"></a>OwnerOnly

```csharp
public bool OwnerOnly { get; set; }
```

When true, the authority accepts the command from a connection only if its trust owns the scope (`Trust.Own`) and its player is the target entity's owner ([`OwnerKeyAttribute`](OwnerKeyAttribute.md)). Others are refused before the handler runs. Needs an [`OwnerKeyAttribute`](OwnerKeyAttribute.md) property. Commands sent from the authority's own code are not checked.

**Type:** `bool`

### <a id="m-predicted"></a>Predicted

```csharp
public bool Predicted { get; set; }
```

When true, a replica that has prediction enabled runs this command on its own predicted copy of the scope the moment it is sent, and again (rolled back and resimulated) whenever the authority's state arrives, until the authority has answered. The handler must only read and write what that replica holds: analyzers report reads of members the audience cannot see (OG0020), random draws without [`CommandAttribute.UsesRandom`](CommandAttribute.md#m-usesrandom) (OG0021) and writes to hidden members (OG0022).

**Type:** `bool`

### <a id="m-usesrandom"></a>UsesRandom

```csharp
public bool UsesRandom { get; set; }
```

Says that a [`CommandAttribute.Predicted`](CommandAttribute.md#m-predicted) handler draws from a named random stream. Replicas hold the streams only when the scope sends them (not `RandomStreams = Hidden`); when they do not, the command is sent without being predicted.

**Type:** `bool`
