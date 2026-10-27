namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] PageOps =>
    [
        new("pages",
            "List the shown OneRender page screens as JSON: {screens [{name, kind (html|tree), order, visible, creations, viewport {width, height}, animating}], renderer {passes, instances, layers, dropped, missingImages, refusedGlyphs, pathBitmaps, artPages, glyphPages}, clockMs}. Pages are view-only. The ui_query, ui_click and ui_invoke operations, and the scenario click:/ui: steps, find page elements by name (an HTML id or form-control name, or '#name') when the OneGame.UI tree has no element of that name; ui_click then sends real pointer events at the element's centre.",
            """{"type":"object","properties":{}}""", false),
    ];
}
