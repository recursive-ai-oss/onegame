[API reference](../../index.md) › [OneGame.Platform](../index.md) › OneGame.Platform

# ActionSource class

**Namespace:** `OneGame.Platform`  
**Package:** [`RecursiveAI.OneGame.Platform`](../index.md)

```csharp
public sealed class ActionSource
```

Another producer of action values next to the devices: Steam Input, the harness, a replay. Values stay until changed, and the strongest of all sources wins.

## Properties

| Member | Description |
| --- | --- |
| [`Name`](#m-name) | The source's label. |

### <a id="m-name"></a>Name

```csharp
public string Name { get; }
```

The source's label.

**Type:** `string`

## Methods

| Member | Description |
| --- | --- |
| [`Clear()`](#m-clear) | Lets go of everything this source holds. |
| [`Pulse(string)`](#m-pulse-string) | Presses a button for exactly one frame: it reads held and pressed next [`ActionMap.Update()`](ActionMap.md#m-update-inputstate-int-int), then released. |
| [`Release(string)`](#m-release-string) | Lets go of one action. |
| [`Set(string, bool)`](#m-set-string-bool) | Holds a button down (or an analog action at full) until released. |
| [`Set(string, float, float)`](#m-set-string-float-float) | Holds an axis or vector at a value until changed. |

### <a id="m-clear"></a>Clear()

```csharp
public void Clear();
```

Lets go of everything this source holds.

### <a id="m-pulse-string"></a>Pulse(string)

```csharp
public void Pulse(string action);
```

Presses a button for exactly one frame: it reads held and pressed next [`ActionMap.Update()`](ActionMap.md#m-update-inputstate-int-int), then released.

**Parameters**

- `action` (`string`): The action's name.

### <a id="m-release-string"></a>Release(string)

```csharp
public void Release(string action);
```

Lets go of one action.

**Parameters**

- `action` (`string`): The action's name.

### <a id="m-set-string-bool"></a>Set(string, bool)

```csharp
public void Set(string action, bool down);
```

Holds a button down (or an analog action at full) until released.

**Parameters**

- `action` (`string`): The action's name.
- `down` (`bool`): Down or up.

### <a id="m-set-string-float-float"></a>Set(string, float, float)

```csharp
public void Set(string action, float x, float y = 0f);
```

Holds an axis or vector at a value until changed. Vectors: down is positive y.

**Parameters**

- `action` (`string`): The action's name.
- `x` (`float`): The value, or x.
- `y` (`float`): y for a vector.
