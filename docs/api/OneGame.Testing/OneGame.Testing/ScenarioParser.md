[API reference](../../index.md) › [OneGame.Testing](../index.md) › OneGame.Testing

# ScenarioParser static class

**Namespace:** `OneGame.Testing`  
**Package:** [`RecursiveAI.OneGame.Testing`](../index.md)

```csharp
public static class ScenarioParser
```

Reads scenario files. They are strict: a key the format does not know is an error with its line.

## Methods

| Member | Description |
| --- | --- |
| [`Load(string)`](#m-load-string) | Reads a scenario file. |
| [`Parse(string, string?, string)`](#m-parse-string-string-string) | Reads a scenario from text. |

### <a id="m-load-string"></a>Load(string)

```csharp
public static Scenario Load(string path);
```

Reads a scenario file.

**Parameters**

- `path` (`string`)

**Returns:** [`Scenario`](Scenario.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The file is not a valid scenario.

### <a id="m-parse-string-string-string"></a>Parse(string, string?, string)

```csharp
public static Scenario Parse(string yaml, string? source = null, string defaultName = "scenario");
```

Reads a scenario from text.

**Parameters**

- `yaml` (`string`): The YAML text.
- `source` (`string?`): The file name for messages, or null.
- `defaultName` (`string`): The name to use when the file has none.

**Returns:** [`Scenario`](Scenario.md)

**Exceptions**

- [`ScenarioException`](ScenarioException.md): The text is not a valid scenario.
