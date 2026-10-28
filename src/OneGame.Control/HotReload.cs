using System.Reflection.Metadata;
using OneGame.Control;

// The runtime calls the handler after it applies an edit-and-continue delta (what 'dotnet watch' sends while 'onegame run' is hot).
[assembly: MetadataUpdateHandler(typeof(HotReloadHandler))]

namespace OneGame.Control;

/// <summary>What a game's live control reports about development hot reload: the <c>hot_reload</c> operation.</summary>
/// <param name="Active">Whether the runtime accepts code edits in this process (the game was started by <c>onegame run</c>, which starts it under <c>dotnet watch</c>).</param>
/// <param name="Applied">How many edits the runtime has applied since this process started.</param>
/// <param name="Pending">Whether an applied edit has not yet been shown: the screens are rebuilt on the next frame.</param>
/// <param name="LastTypes">The types the last edit changed.</param>
/// <param name="LastTick">The simulation tick the last edit was shown at, or -1.</param>
/// <param name="State"><c>applied</c> (the last edit is live), <c>rude</c> (the last edit could not be applied to the running game and needs a restart), <c>error</c> (the last edit does not compile), <c>restarted</c> (the game restarted after a rude edit) or <c>idle</c> (no edit yet).</param>
/// <param name="Message">For <c>rude</c>, <c>error</c> and <c>restarted</c>, what happened, ready to show; otherwise null.</param>
/// <param name="Restarts">How many times <c>onegame run</c> has restarted the game for a rude edit.</param>
/// <param name="Resumed">Whether this process loaded the world its predecessor saved before a restart.</param>
/// <param name="ReplaySafe">False once any edit was applied: recordings made across an edit, and hashes compared with a clean run, will not reproduce.</param>
public sealed record HotReloadResult(bool Active, int Applied, bool Pending, string[] LastTypes, long LastTick, string State, string? Message, int Restarts, bool Resumed, bool ReplaySafe);

/// <summary>Receives the runtime's notice that an edit was applied. Present only in development builds, which are the only ones that reference this assembly.</summary>
internal static class HotReloadHandler
{
    private static int s_applied;
    private static int s_shown;
    private static string[] s_types = [];

    internal static int Applied => Volatile.Read(ref s_applied);

    internal static int Shown => Volatile.Read(ref s_shown);

    internal static string[] LastTypes => Volatile.Read(ref s_types);

    internal static void MarkShown(int applied) => Volatile.Write(ref s_shown, applied);

    // Called by the runtime, on the thread that applied the edit. Nothing here touches the game: the game's own thread notices the count
    // change in its loop and rebuilds what it cached.
    internal static void UpdateApplication(Type[]? updatedTypes)
    {
        Volatile.Write(ref s_types, updatedTypes is null ? [] : [.. updatedTypes.Select(t => t.FullName ?? t.Name)]);
        Interlocked.Increment(ref s_applied);
    }
}

public sealed partial class ControlSurface
{
    private long _hotTick = -1;
    private static bool s_resumed;

    /// <summary>
    /// After <c>onegame run</c> restarted the game for an edit that could not be applied live, loads the world it saved first (named by
    /// <c>ONEGAME_HOT_RESUME</c>), through the generated snapshot migrations, so play continues from the same state. Does nothing otherwise.
    /// </summary>
    /// <returns>Whether a saved world was loaded.</returns>
    public bool ResumeFromHotRestart()
    {
        var path = Environment.GetEnvironmentVariable("ONEGAME_HOT_RESUME");
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return false;
        }

        try
        {
            Game.LoadSnapshot(File.ReadAllBytes(path));
            s_resumed = true;
        }
        catch (Exception ex) when (ex is IOException or InvalidDataException or UnauthorizedAccessException)
        {
            Console.Error.WriteLine("hot reload: the saved world could not be loaded (" + ex.Message + "); starting fresh.");
        }
        finally
        {
            File.Delete(path);
        }

        return s_resumed;
    }

    /// <summary>
    /// Shows an applied code edit: declares the screens again from the new code and rebuilds the open one, and starts a new segment in a
    /// running session recording (an edit may have changed the simulation, so the recording continues from the state the edit met). The
    /// game's loop calls it every frame, next to <see cref="PollShaders"/>; it does nothing when no edit arrived.
    /// </summary>
    public void PollHotReload()
    {
        var applied = HotReloadHandler.Applied;
        if (applied == HotReloadHandler.Shown)
        {
            return;
        }

        // A recording cannot restart from a state with queued commands; try again after the next tick.
        if (Game.World.PendingCommandCount > 0)
        {
            return;
        }

        HotReloadHandler.MarkShown(applied);
        Game.EditWorld(() => true);
        try
        {
            Game.RefreshScreens();
        }
        catch (Exception ex) when (ex is InvalidOperationException or KeyNotFoundException or ArgumentException)
        {
            _hotError = "The edited screens could not be rebuilt: " + ex.Message;
            return;
        }

        _hotError = null;
        _hotTick = Game.World.Tick;
    }

    private string? _hotError;

    private HotReloadResult HotReload()
    {
        PollHotReload();
        var (state, message, restarts, resumed) = ReadWatchStatus();
        if (_hotError is not null)
        {
            (state, message) = ("error", _hotError);
        }
        else if (state == "idle" && HotReloadHandler.Applied > 0)
        {
            state = "applied";
        }

        return new HotReloadResult(
            System.Reflection.Metadata.MetadataUpdater.IsSupported, HotReloadHandler.Applied, HotReloadHandler.Applied != HotReloadHandler.Shown,
            HotReloadHandler.LastTypes, _hotTick, state, message, restarts, resumed, HotReloadHandler.Applied == 0);
    }

    // 'onegame run' writes what dotnet watch said (applied, rude edit, build error, restart) to the file named by ONEGAME_HOT_STATUS.
    private static (string State, string? Message, int Restarts, bool Resumed) ReadWatchStatus()
    {
        var path = Environment.GetEnvironmentVariable("ONEGAME_HOT_STATUS");
        var resumed = s_resumed;
        if (string.IsNullOrEmpty(path) || !File.Exists(path))
        {
            return ("idle", null, 0, resumed);
        }

        try
        {
            using var doc = JsonDocument.Parse(File.ReadAllBytes(path), new JsonDocumentOptions { MaxDepth = 4 });
            var root = doc.RootElement;
            return (
                root.TryGetProperty("state", out var s) && s.ValueKind == JsonValueKind.String ? s.GetString() ?? "idle" : "idle",
                root.TryGetProperty("message", out var m) && m.ValueKind == JsonValueKind.String ? m.GetString() : null,
                root.TryGetProperty("restarts", out var r) && r.TryGetInt32(out var n) ? n : 0,
                resumed);
        }
        catch (Exception ex) when (ex is IOException or JsonException or UnauthorizedAccessException)
        {
            return ("idle", null, 0, resumed);
        }
    }
}
