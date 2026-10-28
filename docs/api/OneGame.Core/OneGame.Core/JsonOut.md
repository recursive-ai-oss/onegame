[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Core

# JsonOut class

**Namespace:** `OneGame.Core`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class JsonOut
```

A small JSON writer for state dumps. Output is compact, ordered as written and identical on every platform.

## Constructors

| Member | Description |
| --- | --- |
| [`JsonOut()`](#m-ctor) |  |

### <a id="m-ctor"></a>JsonOut()

```csharp
public JsonOut();
```

## Methods

| Member | Description |
| --- | --- |
| [`BeginArray()`](#m-beginarray) | Starts an array. |
| [`BeginObject()`](#m-beginobject) | Starts an object. |
| [`EndArray()`](#m-endarray) | Ends the current array. |
| [`EndObject()`](#m-endobject) | Ends the current object. |
| [`Name(string)`](#m-name-string) | Writes a property name inside an object; the next call writes its value. |
| [`Raw(string)`](#m-raw-string) | Writes already-formatted JSON, such as a number. |
| [`ToString()`](#m-tostring) | The JSON written so far. |
| [`Value(bool)`](#m-value-bool) | Writes a bool. |
| [`Value(long)`](#m-value-long) | Writes a signed integer. |
| [`Value(string?)`](#m-value-string) | Writes a string. |
| [`Value(ulong)`](#m-value-ulong) | Writes an unsigned integer. |

### <a id="m-beginarray"></a>BeginArray()

```csharp
public void BeginArray();
```

Starts an array.

### <a id="m-beginobject"></a>BeginObject()

```csharp
public void BeginObject();
```

Starts an object.

### <a id="m-endarray"></a>EndArray()

```csharp
public void EndArray();
```

Ends the current array.

### <a id="m-endobject"></a>EndObject()

```csharp
public void EndObject();
```

Ends the current object.

### <a id="m-name-string"></a>Name(string)

```csharp
public void Name(string name);
```

Writes a property name inside an object; the next call writes its value.

**Parameters**

- `name` (`string`)

### <a id="m-raw-string"></a>Raw(string)

```csharp
public void Raw(string json);
```

Writes already-formatted JSON, such as a number.

**Parameters**

- `json` (`string`)

### <a id="m-tostring"></a>ToString()

```csharp
public override string ToString();
```

The JSON written so far.

**Returns:** `string`

### <a id="m-value-bool"></a>Value(bool)

```csharp
public void Value(bool value);
```

Writes a bool.

**Parameters**

- `value` (`bool`)

### <a id="m-value-long"></a>Value(long)

```csharp
public void Value(long value);
```

Writes a signed integer.

**Parameters**

- `value` (`long`)

### <a id="m-value-string"></a>Value(string?)

```csharp
public void Value(string? value);
```

Writes a string.

**Parameters**

- `value` (`string?`)

### <a id="m-value-ulong"></a>Value(ulong)

```csharp
public void Value(ulong value);
```

Writes an unsigned integer.

**Parameters**

- `value` (`ulong`)
