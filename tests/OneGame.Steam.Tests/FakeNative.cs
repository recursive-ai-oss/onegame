using OneGame.Steam.Native;

namespace OneGame.Steam.Tests;

/// <summary>A stand-in for the native library: records calls, keeps stats and cloud files in memory, and plays back queued
/// callbacks and call results the way the manual-dispatch loop delivers them.</summary>
internal sealed class FakeNative : ISteamNative
{
    private readonly Queue<(int Id, byte[] Data)> _callbacks = new();
    private readonly Dictionary<ulong, (int Id, byte[] Data, bool Failed)> _results = [];
    private (int Id, byte[] Data)? _current;

    public SteamInitStatus InitStatus { get; set; } = SteamInitStatus.Ok;
    public string InitMessage { get; set; } = "";
    public bool RestartResult { get; set; }
    public List<string> Calls { get; } = [];
    public int FreeCount { get; private set; }
    public int RunFrameCount { get; private set; }
    public bool ShutDown { get; private set; }

    public HashSet<string> Achievements { get; } = [];
    public HashSet<string> KnownAchievements { get; } = ["ACH_WIN", "ACH_TEN"];
    public Dictionary<string, int> IntStats { get; } = new() { ["wins"] = 3 };
    public Dictionary<string, float> FloatStats { get; } = new() { ["distance"] = 1.5f };
    public SortedDictionary<string, byte[]> Cloud { get; } = new(StringComparer.Ordinal);
    public bool CloudEnabled { get; set; } = true;
    public ulong CloudTotal { get; set; } = 1000;
    public int CloudFailWrites { get; set; }
    public Dictionary<string, string> Presence { get; } = [];
    public string GameLanguage { get; set; } = "german";
    public bool Deck { get; set; }
    public bool BigPicture { get; set; }
    public HashSet<uint> InstalledDlc { get; } = [];
    public ulong NextCall { get; set; } = 77;
    public bool ShortRead { get; set; }

    public void QueueCallback(int id, byte[] data) => _callbacks.Enqueue((id, data));

    public void SetResult(ulong call, int id, byte[] data, bool failed = false) => _results[call] = (id, data, failed);

    public bool RestartAppIfNecessary(uint appId)
    {
        Calls.Add($"restart:{appId}");
        return RestartResult;
    }

    public SteamInitStatus Init(out string message)
    {
        Calls.Add("init");
        message = InitMessage;
        return InitStatus;
    }

    public void Shutdown() => ShutDown = true;

    public int Pipe => 5;

    public void RunFrame() => RunFrameCount++;

    public bool TryGetNextCallback(out int callbackId, out ReadOnlySpan<byte> data)
    {
        Assert.Null(_current); // the previous one must have been freed
        if (_callbacks.Count == 0)
        {
            callbackId = 0;
            data = default;
            return false;
        }

        _current = _callbacks.Dequeue();
        callbackId = _current.Value.Id;
        data = _current.Value.Data;
        return true;
    }

    public void FreeLastCallback()
    {
        Assert.NotNull(_current);
        _current = null;
        FreeCount++;
    }

    public bool GetCallResult(ulong call, Span<byte> buffer, int expectedCallbackId, out bool failed)
    {
        failed = false;
        if (!_results.TryGetValue(call, out var r) || r.Id != expectedCallbackId || buffer.Length < r.Data.Length)
        {
            return false;
        }

        r.Data.CopyTo(buffer);
        failed = r.Failed;
        return true;
    }

    public ulong GetSteamId() => 76561198000000001UL;

    public string GetPersonaName() => "Ada";

    public bool SetRichPresence(string key, string value)
    {
        if (value.Length == 0)
        {
            Presence.Remove(key);
        }
        else
        {
            Presence[key] = value;
        }

        return true;
    }

    public void ClearRichPresence() => Presence.Clear();

    public void ActivateOverlay(string dialog) => Calls.Add($"overlay:{dialog}");

    public void ActivateOverlayToWebPage(string url) => Calls.Add($"overlay-web:{url}");

    public void ActivateOverlayToStore(uint appId) => Calls.Add($"overlay-store:{appId}");

    public bool IsOverlayEnabled() => true;

    public bool IsRunningOnDeck() => Deck;

    public bool IsBigPicture() => BigPicture;

    public uint GetAppId() => 480;

    public string GetUiLanguage() => "english";

    public bool IsSubscribed() => true;

    public bool IsDlcInstalled(uint appId) => InstalledDlc.Contains(appId);

    public string GetGameLanguage() => GameLanguage;

    public int GetBuildId() => 1234;

    public bool SetAchievement(string name)
    {
        Calls.Add($"set-ach:{name}");
        if (!KnownAchievements.Contains(name))
        {
            return false;
        }

        Achievements.Add(name);
        return true;
    }

    public bool ClearAchievement(string name)
    {
        Achievements.Remove(name);
        return KnownAchievements.Contains(name);
    }

    public bool GetAchievement(string name, out bool unlocked)
    {
        unlocked = Achievements.Contains(name);
        return KnownAchievements.Contains(name);
    }

    public bool IndicateAchievementProgress(string name, uint current, uint max)
    {
        Calls.Add($"progress:{name}:{current}/{max}");
        return KnownAchievements.Contains(name);
    }

    public bool StoreStats()
    {
        Calls.Add("store");
        return true;
    }

    public bool SetStat(string name, int value)
    {
        if (!IntStats.ContainsKey(name))
        {
            return false;
        }

        IntStats[name] = value;
        return true;
    }

    public bool SetStat(string name, float value)
    {
        if (!FloatStats.ContainsKey(name))
        {
            return false;
        }

        FloatStats[name] = value;
        return true;
    }

    public bool GetStat(string name, out int value) => IntStats.TryGetValue(name, out value);

    public bool GetStat(string name, out float value) => FloatStats.TryGetValue(name, out value);

    public ulong RequestNumberOfCurrentPlayers() => NextCall;

    public bool CloudWrite(string name, ReadOnlySpan<byte> data)
    {
        if (CloudFailWrites > 0)
        {
            CloudFailWrites--;
            return false;
        }

        Cloud[name] = data.ToArray();
        return true;
    }

    public int CloudRead(string name, Span<byte> buffer)
    {
        if (!Cloud.TryGetValue(name, out var bytes))
        {
            return 0;
        }

        var n = Math.Min(bytes.Length, buffer.Length) - (ShortRead ? 1 : 0);
        bytes.AsSpan(0, n).CopyTo(buffer);
        return n;
    }

    public bool CloudExists(string name) => Cloud.ContainsKey(name);

    public bool CloudDelete(string name) => Cloud.Remove(name);

    public int CloudSize(string name) => Cloud.TryGetValue(name, out var b) ? b.Length : 0;

    public int CloudFileCount() => Cloud.Count;

    public string CloudFileName(int index, out int size)
    {
        var entry = Cloud.ElementAt(index);
        size = entry.Value.Length;
        return entry.Key;
    }

    public bool CloudQuota(out ulong total, out ulong available)
    {
        total = CloudTotal;
        available = CloudTotal - (ulong)Cloud.Values.Sum(v => v.Length);
        return true;
    }

    public bool CloudEnabledForAccount() => CloudEnabled;

    public bool CloudEnabledForApp() => CloudEnabled;
}
