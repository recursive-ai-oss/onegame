using OneGame.Platform;
using OneGame.UI.Kit;

namespace OneGame.Host;

/// <summary>Lets the UI kit's text fields use the platform clipboard.</summary>
internal sealed class SystemClipboard(PlatformClipboard clipboard) : IClipboard
{
    public string? GetText() => clipboard.GetText();

    public void SetText(string text) => clipboard.SetText(text);
}
