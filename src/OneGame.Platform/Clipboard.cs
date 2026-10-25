using System.Text;

namespace OneGame.Platform;

/// <summary>Which system selection a clipboard call talks to.</summary>
public enum ClipboardSelection : byte
{
    /// <summary>The ordinary clipboard: copy and paste.</summary>
    Clipboard,

    /// <summary>The "primary" selection of X11: the text last highlighted, pasted with the middle button. Other systems do not have it.</summary>
    Primary,
}

/// <summary>
/// Text on the system clipboard. Reads and writes are synchronous and only for UTF-8 text; use it from the thread that runs the
/// <see cref="PlatformApp"/>. Text another program owns is fetched from it on demand, so a read can wait (up to about a second) for a slow
/// owner, and it is served to other programs while the application keeps pumping events.
/// </summary>
public sealed class PlatformClipboard
{
    /// <summary>The longest text read or written, in UTF-8 bytes. Longer clipboard content is treated as absent.</summary>
    public const int MaxTextBytes = 4 * 1024 * 1024;

    private readonly PlatformBackend _backend;

    internal PlatformClipboard(PlatformBackend backend) => _backend = backend;

    /// <summary>True when the system has a clipboard the application can reach. A headless run keeps a private in-process clipboard, so it is true there; it is false on a Wayland compositor with no data-device support.</summary>
    public bool IsAvailable => _backend.ClipboardAvailable;

    /// <summary>Whether <paramref name="selection"/> exists on this system.</summary>
    public bool Supports(ClipboardSelection selection) => _backend.ClipboardSupports(selection);

    /// <summary>The text on the clipboard, or null when there is none, it is not text, it is too long, or its owner did not answer in time.</summary>
    public string? GetText(ClipboardSelection selection = ClipboardSelection.Clipboard) =>
        Supports(selection) ? _backend.ClipboardGet(selection) : null;

    /// <summary>Puts text on the clipboard and keeps serving it until another program takes the clipboard. Returns false when the system refused or the text is longer than <see cref="MaxTextBytes"/>.</summary>
    public bool SetText(string text, ClipboardSelection selection = ClipboardSelection.Clipboard)
    {
        ArgumentNullException.ThrowIfNull(text);
        return Supports(selection) && Encoding.UTF8.GetByteCount(text) <= MaxTextBytes && _backend.ClipboardSet(selection, text);
    }
}

/// <summary>Text conversions the clipboard backends share. They hold no state, so the logic is testable on any host.</summary>
internal static class ClipboardText
{
    private static readonly UTF8Encoding Utf8 = new(false, false);

    /// <summary>Decodes UTF-8 from another program. Invalid sequences become U+FFFD; NULs end the text (C programs append one).</summary>
    public static string FromUtf8(ReadOnlySpan<byte> bytes)
    {
        var nul = bytes.IndexOf((byte)0);
        if (nul >= 0)
        {
            bytes = bytes[..nul];
        }

        return Utf8.GetString(bytes);
    }

    /// <summary>Decodes ISO 8859-1, the "STRING" selection target.</summary>
    public static string FromLatin1(ReadOnlySpan<byte> bytes)
    {
        var nul = bytes.IndexOf((byte)0);
        if (nul >= 0)
        {
            bytes = bytes[..nul];
        }

        return string.Create(bytes.Length, bytes.ToArray(), static (span, b) =>
        {
            for (var i = 0; i < span.Length; i++)
            {
                span[i] = (char)b[i];
            }
        });
    }

    /// <summary>Encodes as ISO 8859-1; characters outside it become '?'.</summary>
    public static byte[] ToLatin1(string text)
    {
        var bytes = new byte[text.Length];
        for (var i = 0; i < text.Length; i++)
        {
            bytes[i] = text[i] <= 0xFF ? (byte)text[i] : (byte)'?';
        }

        return bytes;
    }

    /// <summary>Windows text uses CR LF line ends; converts lone LF (and lone CR) to CR LF.</summary>
    public static string ToCrLf(string text)
    {
        if (text.IndexOfAny(['\r', '\n']) < 0)
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 8);
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if (c == '\r')
            {
                sb.Append("\r\n");
                if (i + 1 < text.Length && text[i + 1] == '\n')
                {
                    i++;
                }
            }
            else if (c == '\n')
            {
                sb.Append("\r\n");
            }
            else
            {
                sb.Append(c);
            }
        }

        return sb.ToString();
    }

    /// <summary>Converts CR LF (and lone CR) line ends to LF, the form the rest of the engine uses.</summary>
    public static string FromCrLf(string text)
    {
        if (text.IndexOf('\r', StringComparison.Ordinal) < 0)
        {
            return text;
        }

        return text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\r', '\n');
    }
}
