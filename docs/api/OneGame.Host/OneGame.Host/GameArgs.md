[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# GameArgs class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public sealed class GameArgs
```

The command line of a game: `--name value`, `--name=value` and bare `--flag`.

## Constructors

| Member | Description |
| --- | --- |
| [`GameArgs(string[])`](#m-ctor-string) | Parses arguments. |

### <a id="m-ctor-string"></a>GameArgs(string\[\])

```csharp
public GameArgs(string[] args);
```

Parses arguments. Anything not starting with `--` and not the value of an option is a positional argument.

**Parameters**

- `args` (`string[]`): The raw arguments.

## Properties

| Member | Description |
| --- | --- |
| [`Names`](#m-names) | The option names given. |
| [`Positional`](#m-positional) | Arguments that are not options. |

### <a id="m-names"></a>Names

```csharp
public IEnumerable<string> Names { get; }
```

The option names given.

**Type:** [`IEnumerable`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ienumerable-1)`<string>`

### <a id="m-positional"></a>Positional

```csharp
public IReadOnlyList<string> Positional { get; }
```

Arguments that are not options.

**Type:** [`IReadOnlyList`](https://learn.microsoft.com/dotnet/api/system.collections.generic.ireadonlylist-1)`<string>`

## Methods

| Member | Description |
| --- | --- |
| [`Get(string)`](#m-get-string) | The option's value, or null if absent or bare. |
| [`GetInt(string, long)`](#m-getint-string-long) | The option as a non-negative integer, or `fallback` when absent. |
| [`Has(string)`](#m-has-string) | Whether the option was given, with or without a value. |

### <a id="m-get-string"></a>Get(string)

```csharp
public string? Get(string name);
```

The option's value, or null if absent or bare.

**Parameters**

- `name` (`string`): The option name without the leading dashes.

**Returns:** `string?`

### <a id="m-getint-string-long"></a>GetInt(string, long)

```csharp
public long GetInt(string name, long fallback);
```

The option as a non-negative integer, or `fallback` when absent.

**Parameters**

- `name` (`string`)
- `fallback` (`long`)

**Returns:** `long`

**Exceptions**

- [`FormatException`](https://learn.microsoft.com/dotnet/api/system.formatexception): The value is not an integer.

### <a id="m-has-string"></a>Has(string)

```csharp
public bool Has(string name);
```

Whether the option was given, with or without a value.

**Parameters**

- `name` (`string`): The option name without the leading dashes.

**Returns:** `bool`
