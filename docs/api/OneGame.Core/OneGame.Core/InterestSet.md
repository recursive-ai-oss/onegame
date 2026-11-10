[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# InterestSet class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class InterestSet
```

The interest keys one connection wants in one scope. An entity whose [`InterestKeyAttribute`](InterestKeyAttribute.md) value is non-zero is sent to the connection only while this set holds it; key 0 entities (and entities without a key) are always sent. The authority owns the set and changes it between ticks; the connection never decides it.

## Constructors

| Member | Description |
| --- | --- |
| [`InterestSet()`](#m-ctor) |  |

### <a id="m-ctor"></a>InterestSet()

```csharp
public InterestSet();
```

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of keys held. |
| [`Keys`](#m-keys) | The keys held, in no particular order. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of keys held.

**Type:** `int`

### <a id="m-keys"></a>Keys

```csharp
public IEnumerable<uint> Keys { get; }
```

The keys held, in no particular order. Do not change the set while enumerating.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<uint>`

## Methods

| Member | Description |
| --- | --- |
| [`Admits(uint)`](#m-admits-uint) | True when an entity with this key is sent: key 0 is global, otherwise the key must be held. |
| [`Contains(uint)`](#m-contains-uint) | True when `key` is held. |
| [`CopyFrom(InterestSet)`](#m-copyfrom-interestset) | Makes this set equal to `other`. |
| [`Replace(ReadOnlySpan<uint>)`](#m-replace-readonlyspan-uint) | Makes the set hold exactly `keys` (duplicates and zeros are ignored). |

### <a id="m-admits-uint"></a>Admits(uint)

```csharp
public bool Admits(uint key);
```

True when an entity with this key is sent: key 0 is global, otherwise the key must be held.

**Parameters**

- `key` (`uint`): The entity's interest key.

**Returns:** `bool`

### <a id="m-contains-uint"></a>Contains(uint)

```csharp
public bool Contains(uint key);
```

True when `key` is held.

**Parameters**

- `key` (`uint`): An interest key.

**Returns:** `bool`

### <a id="m-copyfrom-interestset"></a>CopyFrom(InterestSet)

```csharp
public void CopyFrom(InterestSet other);
```

Makes this set equal to `other`.

**Parameters**

- `other` ([`InterestSet`](InterestSet.md)): The set to copy.

### <a id="m-replace-readonlyspan-uint"></a>Replace(ReadOnlySpan&lt;uint&gt;)

```csharp
public bool Replace(ReadOnlySpan<uint> keys);
```

Makes the set hold exactly `keys` (duplicates and zeros are ignored). Allocation-free once the set has grown to its working size.

**Parameters**

- `keys` ([`ReadOnlySpan`](https://learn.microsoft.com/dotnet/api/system.readonlyspan-1)`<uint>`): The new contents.

**Returns:** `bool` — True when the set changed.
