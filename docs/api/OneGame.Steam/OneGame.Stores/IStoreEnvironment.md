[API reference](../../index.md) › [OneGame.Steam](../index.md) › OneGame.Stores

# IStoreEnvironment interface

**Namespace:** `OneGame.Stores`  
**Package:** [`RecursiveAI.OneGame.Steam`](../index.md)

```csharp
public interface IStoreEnvironment
```

Language and device facts reported by the store.

## Properties

| Member | Description |
| --- | --- |
| [`IsCouchMode`](#m-iscouchmode) | True when the store's couch-mode interface (Big Picture) is showing. |
| [`IsHandheld`](#m-ishandheld) | True on a handheld the store knows about (Steam Deck): use controller-first layouts and larger text. |
| [`Language`](#m-language) | The language the player chose for this game, as the store names it (for Steam, `english`, `german`, `schinese`…). |
| [`LanguageTag`](#m-languagetag) | The same language as a BCP 47 tag (`en`, `de`, `zh-CN`, `pt-BR`…), using the codes in Steam's language table, or empty when unknown or unmapped. |

### <a id="m-iscouchmode"></a>IsCouchMode

```csharp
bool IsCouchMode { get; }
```

True when the store's couch-mode interface (Big Picture) is showing.

**Type:** `bool`

### <a id="m-ishandheld"></a>IsHandheld

```csharp
bool IsHandheld { get; }
```

True on a handheld the store knows about (Steam Deck): use controller-first layouts and larger text.

**Type:** `bool`

### <a id="m-language"></a>Language

```csharp
string Language { get; }
```

The language the player chose for this game, as the store names it (for Steam, `english`, `german`, `schinese`…). Empty when unknown.

**Type:** `string`

### <a id="m-languagetag"></a>LanguageTag

```csharp
string LanguageTag { get; }
```

The same language as a BCP 47 tag (`en`, `de`, `zh-CN`, `pt-BR`…), using the codes in Steam's language table, or empty when unknown or unmapped.

**Type:** `string`
