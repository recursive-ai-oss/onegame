[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# HotShaders class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class HotShaders
```

The shaders of a running game that may be recompiled when their files change. Only development builds act on it.

## Constructors

| Member | Description |
| --- | --- |
| [`HotShaders()`](#m-ctor) |  |

### <a id="m-ctor"></a>HotShaders()

```csharp
public HotShaders();
```

## Properties

| Member | Description |
| --- | --- |
| [`Items`](#m-items) | The registered shaders, in registration order. |

### <a id="m-items"></a>Items

```csharp
public IReadOnlyList<HotShader> Items { get; }
```

The registered shaders, in registration order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)&lt;[`HotShader`](HotShader.md)&gt;

## Methods

| Member | Description |
| --- | --- |
| [`Register(HotShader)`](#m-register-hotshader) | Registers a shader for hot reload. |
| [`Unregister(HotShader)`](#m-unregister-hotshader) | Removes a registration, for example when the material is disposed. |

### <a id="m-register-hotshader"></a>Register(HotShader)

```csharp
public HotShader Register(HotShader shader);
```

Registers a shader for hot reload.

**Parameters**

- `shader` ([`HotShader`](HotShader.md)): The shader.

**Returns:** [`HotShader`](HotShader.md) — The registration, which remembers the file as it is now. Pass it to [`HotShaders.Unregister()`](HotShaders.md#m-unregister-hotshader).

### <a id="m-unregister-hotshader"></a>Unregister(HotShader)

```csharp
public bool Unregister(HotShader shader);
```

Removes a registration, for example when the material is disposed.

**Parameters**

- `shader` ([`HotShader`](HotShader.md)): The shader returned by registration.

**Returns:** `bool` — True when it was registered.
