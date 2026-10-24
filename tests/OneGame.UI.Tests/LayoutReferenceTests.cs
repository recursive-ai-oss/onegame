using System.Text.Json;

namespace OneGame.UI.Tests;

/// <summary>
/// Layout against boxes measured in headless Chrome. The cases are in testdata/ui-layout/cases.html and the expected boxes in
/// expected.json, regenerated with tools/ui-layout-reference/generate.py.
/// </summary>
public class LayoutReferenceTests
{
    private static readonly Dictionary<string, (int W, int H, string Html)> Cases = LoadCases();

    public static IEnumerable<TheoryDataRow<string>> CaseNames => Cases.Keys.Select(k => new TheoryDataRow<string>(k));

    private static Dictionary<string, (int, int, string)> LoadCases()
    {
        var result = new Dictionary<string, (int, int, string)>();
        string? name = null;
        int w = 0, h = 0;
        var body = new System.Text.StringBuilder();
        foreach (var line in File.ReadAllLines(Path.Combine(Paths.TestData(), "cases.html")))
        {
            if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                if (name is not null)
                {
                    result[name] = (w, h, body.ToString());
                }

                var parts = line[3..].Split(' ');
                name = parts[0];
                var size = parts[1].Split('x');
                (w, h) = (int.Parse(size[0], System.Globalization.CultureInfo.InvariantCulture), int.Parse(size[1], System.Globalization.CultureInfo.InvariantCulture));
                body.Clear();
            }
            else
            {
                body.Append(line);
            }
        }

        result[name!] = (w, h, body.ToString());
        return result;
    }

    [Fact]
    public void EveryCaseHasAnExpectedResult()
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Paths.TestData(), "expected.json")));
        Assert.Equal(Cases.Count, doc.RootElement.EnumerateObject().Count());
        Assert.True(Cases.Count >= 60);
    }

    [Theory]
    [MemberData(nameof(CaseNames))]
    public void MatchesChrome(string name)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(Path.Combine(Paths.TestData(), "expected.json")));
        var expected = doc.RootElement.GetProperty(name);
        var (w, h, html) = Cases[name];

        var ui = new UiTree();
        ui.SetViewport(w, h);
        var root = new Element("Case").Style(s => s.Size(w, h).Direction(FlexDirection.Column));
        foreach (var e in MiniHtml.Parse(html))
        {
            root.Add(e);
        }

        ui.Root.Add(root);
        ui.Frame();

        var problems = new List<string>();
        foreach (var box in expected.EnumerateObject())
        {
            var el = Find(root, box.Name);
            if (el is null)
            {
                problems.Add($"{box.Name}: element missing");
                continue;
            }

            var r = el.AbsoluteBounds;
            var want = box.Value.EnumerateArray().Select(v => v.GetSingle()).ToArray();
            float[] got = [r.X, r.Y, r.Width, r.Height];
            for (var i = 0; i < 4; i++)
            {
                if (MathF.Abs(got[i] - want[i]) > 0.05f)
                {
                    problems.Add($"{box.Name}: x,y,w,h expected [{string.Join(", ", want)}] got [{string.Join(", ", got.Select(g => MathF.Round(g, 3)))}]");
                    break;
                }
            }
        }

        Assert.True(problems.Count == 0, name + "\n" + string.Join("\n", problems));
    }

    private static Element? Find(Element e, string id)
    {
        if (e.Id == id)
        {
            return e;
        }

        for (var i = 0; i < e.ChildCount; i++)
        {
            if (Find(e.ChildAt(i), id) is { } f)
            {
                return f;
            }
        }

        return null;
    }
}
