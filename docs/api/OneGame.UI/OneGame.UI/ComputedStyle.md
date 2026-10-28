[API reference](../../index.md) › [OneGame.UI](../index.md) › OneGame.UI

# ComputedStyle class

**Namespace:** `OneGame.UI`  
**Package:** [`RecursiveAI.OneGame.UI`](../index.md)

```csharp
public sealed class ComputedStyle
```

The resolved value of every style property of one element, after the cascade, inheritance and theme tokens.

## Properties

| Member | Description |
| --- | --- |
| [`PropertyNames`](#m-propertynames) | The CSS names of every property, in a fixed order. |

### <a id="m-propertynames"></a>PropertyNames

```csharp
public static IReadOnlyList<string> PropertyNames { get; }
```

The CSS names of every property, in a fixed order.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`Get(string)`](#m-get-string) | The value of a property in CSS spelling, or null when the name is unknown. |

### <a id="m-get-string"></a>Get(string)

```csharp
public string? Get(string cssName);
```

The value of a property in CSS spelling, or null when the name is unknown.

**Parameters**

- `cssName` (`string`): A property name such as `flex-direction`.

**Returns:** `string?`
