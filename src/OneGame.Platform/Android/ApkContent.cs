namespace OneGame.Platform;

/// <summary>
/// Unpacks the game's content files (art, fonts, data) from the APK into the app's files folder, which is the game's working
/// folder, so the game finds them the same way it finds loose files beside the program on a computer. <c>onegame export</c> stores
/// them under <c>assets/</c> with a list, <c>assets/onegame-content.txt</c>, because a native activity can read the APK only through
/// the asset manager and cannot list a folder.
/// </summary>
internal static unsafe class ApkContent
{
    internal const string ListName = "onegame-content.txt";

    // The list and each file are bounded: a damaged APK must not exhaust memory.
    private const long MaxFile = 512L * 1024 * 1024;
    private const int MaxFiles = 100_000;

    /// <summary>Copies every listed file to <paramref name="target"/>; returns how many. A missing list means a build with no content.</summary>
    public static int Extract(nint assetManager, string target)
    {
        if (assetManager == 0)
        {
            return 0;
        }

        var list = Read(assetManager, ListName, 16 * 1024 * 1024);
        if (list is null)
        {
            return 0;
        }

        var count = 0;
        foreach (var line in System.Text.Encoding.UTF8.GetString(list).Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            if (count >= MaxFiles || !IsSafe(line))
            {
                continue;
            }

            var data = Read(assetManager, line, MaxFile);
            if (data is null)
            {
                continue;
            }

            var path = Path.Combine(target, line);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllBytes(path, data);
            count++;
        }

        return count;
    }

    // A relative path of plain segments: nothing that climbs out of the files folder.
    internal static bool IsSafe(string path)
    {
        if (path.Length == 0 || path[0] == '/' || path.Contains('\\', StringComparison.Ordinal))
        {
            return false;
        }

        foreach (var segment in path.Split('/'))
        {
            if (segment.Length == 0 || segment is "." or "..")
            {
                return false;
            }
        }

        return true;
    }

    private static byte[]? Read(nint manager, string name, long limit)
    {
        var asset = AndroidNative.AAssetManager_open(manager, name, 2); // AASSET_MODE_BUFFER
        if (asset == 0)
        {
            return null;
        }

        try
        {
            var length = AndroidNative.AAsset_getLength64(asset);
            if (length < 0 || length > limit)
            {
                return null;
            }

            var data = new byte[length];
            fixed (byte* p = data)
            {
                var done = 0L;
                while (done < length)
                {
                    var n = AndroidNative.AAsset_read(asset, p + done, (nuint)(length - done));
                    if (n <= 0)
                    {
                        return null;
                    }

                    done += n;
                }
            }

            return data;
        }
        finally
        {
            AndroidNative.AAsset_close(asset);
        }
    }
}
