namespace OneGame.UI.Kit;

public static partial class UiKit
{
    /// <summary>
    /// A tab view (kind <c>Tabs</c>): a row of tabs above the content of the selected page. Pages stay in the tree while hidden, so their state (scroll position, text typed) survives.
    /// Tabs are kind <c>Tab</c>, role <c>tab</c>, ids <c>id/tab/0</c>...; the strip is role <c>tablist</c> (<c>id/list</c>); each panel is role <c>tabpanel</c> (<c>id/panel/0</c>...).
    /// Only the selected tab takes Tab focus. With focus on a tab, Left/Right (arrow keys or d-pad) select and focus the neighbor; Ctrl+PageUp and Ctrl+PageDown switch tabs from anywhere inside.
    /// </summary>
    /// <param name="pages">The pages, in tab order.</param>
    /// <param name="selected">Reads the selected page index, every frame.</param>
    /// <param name="select">Called with the index of the tab the player picked.</param>
    /// <param name="id">The id, or null.</param>
    public static Element Tabs(IReadOnlyList<TabPage> pages, Func<int> selected, Action<int> select, string? id = null)
    {
        ArgumentNullException.ThrowIfNull(pages);
        ArgumentNullException.ThrowIfNull(selected);
        ArgumentNullException.ThrowIfNull(select);
        var root = Make("Tabs", "group", "kit-tabs", id);
        var strip = Make("TabList", "tablist", "kit-tablist", null);
        strip.Id = id is null ? null : id + "/list";
        var panelHost = Part("Box", "kit-tabpanels", id, "panels");
        var tabs = new Element[pages.Count];
        var panels = new Element[pages.Count];
        for (var i = 0; i < tabs.Length; i++)
        {
            var index = i;
            var page = pages[i];
            var tab = Make("Tab", "tab", "kit-tab", page.Id ?? (id is null ? null : id + "/tab/" + Invariant(i)), focusable: true);
            tab.Value = page.Title;
            tab.Add(Ui.Label(page.Title).Class("kit-tab-label"));
            tab.OnClick(() =>
            {
                select(index);
                // Only the selected tab is focusable, so a click on another tab would otherwise drop focus.
                tab.IsFocusable = true;
                tab.Tree?.Focus(tab);
            });
            tabs[i] = tab;
            strip.Add(tab);
            var panel = Make("TabPanel", "tabpanel", "kit-tabpanel", id is null ? null : id + "/panel/" + Invariant(i));
            panel.Add(page.Content);
            panels[i] = panel;
            panelHost.Add(panel);
        }

        root.With(strip, panelHost);
        root.OnUpdate(r =>
        {
            var sel = tabs.Length == 0 ? -1 : Math.Clamp(selected(), 0, tabs.Length - 1);
            for (var i = 0; i < tabs.Length; i++)
            {
                var on = i == sel;
                tabs[i].SetClass("selected", on);
                tabs[i].IsFocusable = on;
                panels[i].IsVisible = on;
            }

            r.Value = sel < 0 ? null : pages[sel].Title;
        });
        NavigationKeys.OnStep(strip, true, delta =>
        {
            var focused = root.Tree?.Focused;
            var at = focused is null ? -1 : Array.IndexOf(tabs, focused);
            var to = at + delta;
            if (at < 0 || to < 0 || to >= tabs.Length)
            {
                return false;
            }

            tabs[to].IsFocusable = true;
            select(to);
            root.Tree!.Focus(tabs[to]);
            return true;
        });
        root.On(UiEventKind.KeyDown, (_, e) =>
        {
            if ((e.Modifiers & KeyModifiers.Ctrl) == 0 || tabs.Length == 0)
            {
                return false;
            }

            var step = IsKey(e, "PageDown") ? 1 : IsKey(e, "PageUp") ? -1 : 0;
            var cur = Math.Clamp(selected(), 0, tabs.Length - 1);
            var to = cur + step;
            if (step == 0 || to < 0 || to >= tabs.Length)
            {
                return false;
            }

            select(to);
            return true;
        });
        return root;
    }
}
