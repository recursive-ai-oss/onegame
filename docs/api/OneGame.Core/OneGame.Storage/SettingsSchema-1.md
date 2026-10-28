[API reference](../../index.md) › [OneGame.Core](../index.md) › OneGame.Storage

# SettingsSchema&lt;T&gt; class

**Namespace:** `OneGame.Storage`  
**Package:** [`RecursiveAI.OneGame.Core`](../index.md)

```csharp
public sealed class SettingsSchema<T> where T : class, new()
```

Describes a settings class: which properties are saved, under which keys, and the range each one may take. The defaults are the values a fresh `T` has. Built once with the fluent methods, no reflection, so it is trim and AOT safe.

**Type parameters**

- `T`: The settings class: a class with a public parameterless constructor holding the player's choices.

## Constructors

| Member | Description |
| --- | --- |
| [`SettingsSchema(string, int)`](#m-ctor-string-int) | Creates a schema stored in the file `fileName` (default `settings.bin`). |

### <a id="m-ctor-string-int"></a>SettingsSchema(string, int)

```csharp
public SettingsSchema(string fileName = "settings.bin", int version = 1);
```

Creates a schema stored in the file `fileName` (default `settings.bin`).

**Parameters**

- `fileName` (`string`): A valid store name.
- `version` (`int`): Your own settings version, saved in the file; read it from [`SettingsLoad.FileVersion`](SettingsLoad.md#m-fileversion) to migrate.

## Properties

| Member | Description |
| --- | --- |
| [`Count`](#m-count) | The number of saved fields. |
| [`FileName`](#m-filename) | The file the settings are kept in. |
| [`Version`](#m-version) | The version written into the file. |

### <a id="m-count"></a>Count

```csharp
public int Count { get; }
```

The number of saved fields.

**Type:** `int`

### <a id="m-filename"></a>FileName

```csharp
public string FileName { get; }
```

The file the settings are kept in.

**Type:** `string`

### <a id="m-version"></a>Version

```csharp
public int Version { get; }
```

The version written into the file.

**Type:** `int`

## Methods

| Member | Description |
| --- | --- |
| [`Choice<TEnum>(string, Func<T, TEnum>, Action<T, TEnum>)`](#m-choice-tenum-string-func-t-tenum-action-t-tenum) | Adds a choice from an enum with a 32-bit underlying type. |
| [`Flag(string, Func<T, bool>, Action<T, bool>)`](#m-flag-string-func-t-bool-action-t-bool) | Adds an on/off choice. |
| [`Number(string, Func<T, float>, Action<T, float>, float, float)`](#m-number-string-func-t-float-action-t-float-float-float) | Adds a number with a fraction (a volume, a scale) kept between `min` and `max`. |
| [`Text(string, Func<T, string>, Action<T, string>, int)`](#m-text-string-func-t-string-action-t-string-int) | Adds text of at most `maxLength` UTF-8 bytes (a player name, a language code). |
| [`Whole(string, Func<T, int>, Action<T, int>, int, int)`](#m-whole-string-func-t-int-action-t-int-int-int) | Adds a whole number kept between `min` and `max` (a value outside it loads as the default). |

### <a id="m-choice-tenum-string-func-t-tenum-action-t-tenum"></a>Choice&lt;TEnum&gt;(string, Func&lt;T, TEnum&gt;, Action&lt;T, TEnum&gt;)

```csharp
public SettingsSchema<T> Choice<TEnum>(string key, Func<T, TEnum> get, Action<T, TEnum> set) where TEnum : unmanaged, ValueType, Enum;
```

Adds a choice from an enum with a 32-bit underlying type. A number that is not a member loads as the default.

**Parameters**

- `key` (`string`)
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, TEnum>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, TEnum>`)

**Returns:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`

### <a id="m-flag-string-func-t-bool-action-t-bool"></a>Flag(string, Func&lt;T, bool&gt;, Action&lt;T, bool&gt;)

```csharp
public SettingsSchema<T> Flag(string key, Func<T, bool> get, Action<T, bool> set);
```

Adds an on/off choice.

**Parameters**

- `key` (`string`)
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, bool>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, bool>`)

**Returns:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`

### <a id="m-number-string-func-t-float-action-t-float-float-float"></a>Number(string, Func&lt;T, float&gt;, Action&lt;T, float&gt;, float, float)

```csharp
public SettingsSchema<T> Number(string key, Func<T, float> get, Action<T, float> set, float min = -3.4028235E+38f, float max = 3.4028235E+38f);
```

Adds a number with a fraction (a volume, a scale) kept between `min` and `max`. NaN and infinities load as the default.

**Parameters**

- `key` (`string`)
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, float>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, float>`)
- `min` (`float`)
- `max` (`float`)

**Returns:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`

### <a id="m-text-string-func-t-string-action-t-string-int"></a>Text(string, Func&lt;T, string&gt;, Action&lt;T, string&gt;, int)

```csharp
public SettingsSchema<T> Text(string key, Func<T, string> get, Action<T, string> set, int maxLength = 256);
```

Adds text of at most `maxLength` UTF-8 bytes (a player name, a language code). Longer text loads as the default.

**Parameters**

- `key` (`string`)
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, string>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, string>`)
- `maxLength` (`int`)

**Returns:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`

### <a id="m-whole-string-func-t-int-action-t-int-int-int"></a>Whole(string, Func&lt;T, int&gt;, Action&lt;T, int&gt;, int, int)

```csharp
public SettingsSchema<T> Whole(string key, Func<T, int> get, Action<T, int> set, int min = -2147483648, int max = 2147483647);
```

Adds a whole number kept between `min` and `max` (a value outside it loads as the default).

**Parameters**

- `key` (`string`)
- `get` ([`Func`](https://learn.microsoft.com/dotnet/api/system.func-2)`<T, int>`)
- `set` ([`Action`](https://learn.microsoft.com/dotnet/api/system.action-2)`<T, int>`)
- `min` (`int`)
- `max` (`int`)

**Returns:** [`SettingsSchema`](SettingsSchema-1.md)`<T>`
