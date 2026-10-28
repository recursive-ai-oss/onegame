[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Steam.Input

# ISteamInputDriver interface

**Namespace:** `OneGame.Steam.Input`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface ISteamInputDriver
```

How the harness plays the part of a pad when no Steam is behind Steam Input.

## Methods

| Member | Description |
| --- | --- |
| [`Connect(int)`](#m-connect-int) | Plugs in a controller of a Steam input type (3 = Xbox One). |
| [`Disconnect(int)`](#m-disconnect-int) | Unplugs a controller. |
| [`SetAnalog(int, string, float, float)`](#m-setanalog-int-string-float-float) | Sets an analog action's value. |
| [`SetDigital(int, string, bool)`](#m-setdigital-int-string-bool) | Sets a digital action's state, as the action sets bound on the pad would. |

### <a id="m-connect-int"></a>Connect(int)

```csharp
int Connect(int inputType);
```

Plugs in a controller of a Steam input type (3 = Xbox One).

**Parameters**

- `inputType` (`int`): The input type number.

**Returns:** `int` — Its index.

### <a id="m-disconnect-int"></a>Disconnect(int)

```csharp
void Disconnect(int index);
```

Unplugs a controller.

**Parameters**

- `index` (`int`): Its index.

### <a id="m-setanalog-int-string-float-float"></a>SetAnalog(int, string, float, float)

```csharp
bool SetAnalog(int index, string action, float x, float y);
```

Sets an analog action's value.

**Parameters**

- `index` (`int`): The controller.
- `action` (`string`): The action's name.
- `x` (`float`): First axis.
- `y` (`float`): Second axis (up positive).

**Returns:** `bool` — False when the action is not in the active set.

### <a id="m-setdigital-int-string-bool"></a>SetDigital(int, string, bool)

```csharp
bool SetDigital(int index, string action, bool down);
```

Sets a digital action's state, as the action sets bound on the pad would.

**Parameters**

- `index` (`int`): The controller.
- `action` (`string`): The action's name.
- `down` (`bool`): Down or up.

**Returns:** `bool` — False when the action is not in the active set (a real pad would report it inactive).
