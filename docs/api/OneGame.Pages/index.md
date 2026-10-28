[API reference](../index.md) › OneGame.Pages

# OneGame.Pages

Draws OneRender pages (builder trees and HTML) through the render graph, with input, host scopes and harness support.

**Package:** `RecursiveAI.OneGame.Pages`

```sh
dotnet add package RecursiveAI.OneGame.Pages
```

## OneGame.Pages

| Type | Kind | Description |
| --- | --- | --- |
| [`PageFonts`](OneGame.Pages/PageFonts.md) | class | The fonts of a page, shared with the game's [`FontLibrary`](../OneGame.Text/OneGame.Text/FontLibrary.md): each face is loaded once and the same `FontFace` object sits in OneRender's catalog and in the library, so a page and a `OneGame.UI` screen shape and draw with identical faces and glyph bitmaps. |
| [`PageFrame`](OneGame.Pages/PageFrame.md) | struct | How a frame of pages is drawn. |
| [`PageImages`](OneGame.Pages/PageImages.md) | class | The images a page can show. |
| [`PageOptions`](OneGame.Pages/PageOptions.md) | class | What a page is made from. |
| [`PageRenderer`](OneGame.Pages/PageRenderer.md) | class | The GPU side of pages: turns display lists into passes of a render graph. |
| [`PageScopeBinding`](OneGame.Pages/PageScopeBinding.md) | struct | A resolved host-scope binding. |
| [`PageScopeKind`](OneGame.Pages/PageScopeKind.md) | enum | What a host-scope number means to the page renderer. |
| [`PageScopes`](OneGame.Pages/PageScopes.md) | class | Gives the numbers a page sets with `--host-scope` (or `StyleDeclaration.SetHostScope`) their meaning. |
| [`PageScreen`](OneGame.Pages/PageScreen.md) | class | One OneRender page shown as a screen: an HTML document ([`PageScreen.Interactive`](OneGame.Pages/PageScreen.md#m-interactive)) or a builder tree ([`PageScreen.Live`](OneGame.Pages/PageScreen.md#m-live)). |
| [`PageSet`](OneGame.Pages/PageSet.md) | class | The page screens of a game (`game.Pages`): which are shown, in what order, the clock they run on and the input routed to them. |
| [`PageStats`](OneGame.Pages/PageStats.md) | struct | What the last [`PageRenderer.AddPasses()`](OneGame.Pages/PageRenderer.md#m-addpasses-rendergraph-graphtexture-ireadonlylist-pagescreen-pagescopes-pageframe) did. |

## OneGame.Pages.Shaders

| Type | Kind | Description |
| --- | --- | --- |
| [`Page`](OneGame.Pages.Shaders/Page.md) | static class |  |
| [`Page.Bindings`](OneGame.Pages.Shaders/Page.Bindings.md) | static class |  |
| [`Page.Bindings.Clips`](OneGame.Pages.Shaders/Page.Bindings.Clips.md) | static class |  |
| [`Page.Bindings.Frame`](OneGame.Pages.Shaders/Page.Bindings.Frame.md) | static class |  |
| [`Page.Bindings.Samp`](OneGame.Pages.Shaders/Page.Bindings.Samp.md) | static class |  |
| [`Page.Bindings.Tex`](OneGame.Pages.Shaders/Page.Bindings.Tex.md) | static class |  |
| [`Page.Bindings.Tex2`](OneGame.Pages.Shaders/Page.Bindings.Tex2.md) | static class |  |
| [`Page.Frame`](OneGame.Pages.Shaders/Page.Frame.md) | struct |  |
| [`Page.PSBackdrop`](OneGame.Pages.Shaders/Page.PSBackdrop.md) | static class |  |
| [`Page.PSBlend`](OneGame.Pages.Shaders/Page.PSBlend.md) | static class |  |
| [`Page.PSBlur`](OneGame.Pages.Shaders/Page.PSBlur.md) | static class |  |
| [`Page.PSColor`](OneGame.Pages.Shaders/Page.PSColor.md) | static class |  |
| [`Page.PSDropTint`](OneGame.Pages.Shaders/Page.PSDropTint.md) | static class |  |
| [`Page.PSEncode`](OneGame.Pages.Shaders/Page.PSEncode.md) | static class |  |
| [`Page.PSLayer`](OneGame.Pages.Shaders/Page.PSLayer.md) | static class |  |
| [`Page.PSMain`](OneGame.Pages.Shaders/Page.PSMain.md) | static class |  |
| [`Page.PSMaskApply`](OneGame.Pages.Shaders/Page.PSMaskApply.md) | static class |  |
| [`Page.PSMaskCombine`](OneGame.Pages.Shaders/Page.PSMaskCombine.md) | static class |  |
| [`Page.PSOver`](OneGame.Pages.Shaders/Page.PSOver.md) | static class |  |
| [`Page.PSPresent`](OneGame.Pages.Shaders/Page.PSPresent.md) | static class |  |
| [`Page.PSProject`](OneGame.Pages.Shaders/Page.PSProject.md) | static class |  |
| [`Page.VSMain`](OneGame.Pages.Shaders/Page.VSMain.md) | static class |  |
| [`Page.VSMain.Vertex`](OneGame.Pages.Shaders/Page.VSMain.Vertex.md) | struct |  |
