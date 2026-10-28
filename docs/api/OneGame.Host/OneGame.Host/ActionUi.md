[API reference](../../index.md) › [OneGame.Host](../index.md) › OneGame.Host

# ActionUi static class

**Namespace:** `OneGame.Host`  
**Package:** [`RecursiveAI.OneGame.Host`](../index.md)

```csharp
public static class ActionUi
```

Ties the action layer to the UI kit: glyph tables that follow the current bindings, and a ready-made rebinding list.

## Methods

| Member | Description |
| --- | --- |
| [`BindingList(GameInstance, InputGlyphs?, string?)`](#m-bindinglist-gameinstance-inputglyphs-string) | A list of the actions with their current bindings, a glyph for each, a Rebind button and a Reset button. |
| [`SyncGlyphs(ActionMap, InputGlyphs)`](#m-syncglyphs-actionmap-inputglyphs) | Points an [`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md) table at the actions' current bindings, so `UiKit.InputGlyph(glyphs, "jump")` shows the key or button the player really uses. |

### <a id="m-bindinglist-gameinstance-inputglyphs-string"></a>BindingList(GameInstance, InputGlyphs?, string?)

```csharp
public static Element BindingList(GameInstance game, InputGlyphs? glyphs = null, string? context = null);
```

A list of the actions with their current bindings, a glyph for each, a Rebind button and a Reset button. Rebind waits for the player's next key, mouse button, pad button or wheel turn (Escape cancels) and replaces the first binding, taking the source from other actions in the same context. Vectors and pair axes (WASD, sticks, Q/E) show their bindings and can be reset but not recaptured here; build their rows with [`ActionMap.BeginRebind()`](../../OneGame.Platform/OneGame.Platform/ActionMap.md#m-beginrebind-string-int-int-conflictpolicy) if the game wants that. Ids: `rebind-NAME`, `reset-NAME`, `binding-NAME`; the list is `action-bindings`.

**Parameters**

- `game` ([`GameInstance`](GameInstance.md)): The running game; its [`GameInstance.Actions`](GameInstance.md#m-actions) are listed.
- `glyphs` ([`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md)?): The glyph table to show pictures from, or null for text only. The list keeps it in step with the bindings.
- `context` (`string?`): Only this context's actions, or null for all.

**Returns:** [`Element`](../../OneGame.UI/OneGame.UI/Element.md) — The list element.

### <a id="m-syncglyphs-actionmap-inputglyphs"></a>SyncGlyphs(ActionMap, InputGlyphs)

```csharp
public static void SyncGlyphs(ActionMap map, InputGlyphs glyphs);
```

Points an [`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md) table at the actions' current bindings, so `UiKit.InputGlyph(glyphs, "jump")` shows the key or button the player really uses. Each action gets its first keyboard key, mouse button and pad control; a vector shows its up key and the stick or d-pad. Call again after bindings change ([`ActionMap.Version`](../../OneGame.Platform/OneGame.Platform/ActionMap.md#m-version) tells).

**Parameters**

- `map` ([`ActionMap`](../../OneGame.Platform/OneGame.Platform/ActionMap.md)): The game's actions.
- `glyphs` ([`InputGlyphs`](../../OneGame.UI.Kit/OneGame.UI.Kit/InputGlyphs.md)): The glyph table.
