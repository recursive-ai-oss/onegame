using System.Runtime.CompilerServices;
using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Bricks.Views;

/// <summary>
/// Everything the player does that is not a rule: which screen came before, the settings and the high-score table, the sounds, and the
/// keys and buttons that become commands. One session belongs to one running game. It reads the court and sends it commands; it never
/// changes the court itself, and the <see cref="ViewAttribute"/> makes the compiler hold it to that.
/// </summary>
[View]
internal sealed class Session
{
    private const int MaxFileBytes = 4 * 1024;

    private static readonly ConditionalWeakTable<GameInstance, Session> Sessions = new();

    private World? _world;
    private int _resets;
    private int _restarts;
    private long _noticeUntil;
    private string? _notice;
    private CourtPhase _phase;
    private int _hits;
    private int _breaks;
    private int _clinks;
    private int _walls;
    private int _paddles;
    private int _catches;
    private int _drops;
    private int _clears;

    private Session(GameInstance game)
    {
        Game = game;
        Definition = (BricksGame)game.Definition;
        Data = Definition.Services.CreateStore(game);
        Sound = Definition.Services.Sounds;
        Profile = LoadProfile();
        Kit = Kits.For(game.Ui);
        Kit.Glyphs
            .Bind("launch", keyboard: " ", mouse: "left", gamepad: "a")
            .Bind("pause", keyboard: "Escape", gamepad: "start");
        _resets = Definition.Resets;
        _restarts = Definition.Restarts;
        ApplyUiScale();
        game.TickCompleted += OnTick;
        game.WorldReplaced += OnWorldReplaced;
        Resync(game.World);
    }

    public GameInstance Game { get; }

    /// <summary>The component kit's host for this game's UI tree: theme, tooltips and input glyphs.</summary>
    public KitHost Kit { get; }

    public BricksGame Definition { get; }

    /// <summary>The settings and the high-score table.</summary>
    public Profile Profile { get; private set; }

    public IDataStore Data { get; }

    public ISoundSink Sound { get; }

    /// <summary>The screen to return to from the settings and the high scores.</summary>
    public string Previous { get; private set; } = "main";

    /// <summary>True once the score of the game that just ended has been put in the table (or declined).</summary>
    public bool ScoreHandled { get; private set; }

    /// <summary>True when the game that just ended earned a place in the table and has not been entered yet.</summary>
    public bool CanEnterScore => !ScoreHandled && Court.Phase == CourtPhase.Over && Profile.Qualifies(Court.Score);

    /// <summary>The name being typed for the table. It starts empty for each game that ends (an empty name is saved as "Player").</summary>
    public string NameDraft { get; set; } = string.Empty;

    /// <summary>A short message for the hint line, or null.</summary>
    public string? Notice => Game.Frame < _noticeUntil ? _notice : null;

    public Court Court => (Court)Game.World.Entities[0];

    public static Session For(GameInstance game) => Sessions.GetValue(game, static g => new Session(g));

    // ---- screens and the game's flow ---------------------------------------------------------------------------------

    public void Open(string screen)
    {
        if (Game.CurrentScreen is { } current && current != screen && screen is "scores" or "settings")
        {
            Previous = current;
        }

        Game.OpenScreen(screen);
        FocusHelpers.FocusDefault(Game.Ui, Game.Ui.Root);
        Game.Ui.Update();
    }

    public void Back() => Open(Previous);

    public void Click() => Play(SoundEvent.Click);

    public void StartGame()
    {
        ScoreHandled = false;
        Court.Send.NewGame(1);
        Open("play");
    }

    /// <summary>Leaves the game for the title screen. An unfinished game is dropped; one that ended keeps its score entry chance only until now.</summary>
    public void ToTitle()
    {
        Court.Send.Quit();
        Open("main");
    }

    public void TogglePause()
    {
        if (Game.CurrentScreen == "play" && Court.InPlay)
        {
            Court.Send.Pause(!Court.Paused);
        }
    }

    public void Launch()
    {
        if (Game.CurrentScreen == "play" && Court.Phase == CourtPhase.Serve && !Court.Paused)
        {
            Court.Send.Launch();
        }
    }

    // ---- settings and the table --------------------------------------------------------------------------------------

    public void SaveProfile() => Data.Write(Profile.FileName, Profile.Write());

    public void SaveScore(string name)
    {
        if (!CanEnterScore)
        {
            return;
        }

        Profile.Add(name, Court.Score, Court.Level, Court.Victory);
        ScoreHandled = true;
        SaveProfile();
        Say("Score saved.");
    }

    public void ApplyUiScale() => Game.UiScaleOverride = Profile.UiScale;

    public void Say(string message)
    {
        _notice = message;
        _noticeUntil = Game.Frame + 240;
    }

    public void Play(SoundEvent sound)
    {
        if (Profile.Volume > 0)
        {
            Sound.Play(sound, Profile.Volume / 10f);
        }
    }

    private Profile LoadProfile() =>
        Data.TryRead(Profile.FileName, MaxFileBytes, out var bytes) && Profile.TryRead(bytes, out var loaded) ? loaded : new Profile();

    // ---- what the court did: sounds ----------------------------------------------------------------------------------

    private void Resync(World world)
    {
        if (world.Entities.Count == 0 || world.Entities[0] is not Court court)
        {
            return;
        }

        _world = world;
        _phase = court.Phase;
        _hits = court.Hits;
        _breaks = court.Breaks;
        _clinks = court.Clinks;
        _walls = court.WallBounces;
        _paddles = court.PaddleBounces;
        _catches = court.Catches;
        _drops = court.Drops;
        _clears = court.Clears;
        ScoreHandled = court.Phase != CourtPhase.Over;
    }

    private void OnWorldReplaced(World world)
    {
        Resync(world);
        if (Definition.Resets != _resets)
        {
            // The harness reset the game: a new scenario, or a new game of the harness's own. In a run whose files live in memory that
            // means a clean slate, so scenarios never see each other's scores.
            _resets = Definition.Resets;
            if (Data is MemoryStore)
            {
                Data.Delete(Profile.FileName);
            }

            Restart();
        }
        else if (Definition.Restarts != _restarts)
        {
            Restart();
        }
    }

    // Starts over as the program does: everything kept only in memory is dropped and the settings and table are read back from the data store.
    private void Restart()
    {
        _restarts = Definition.Restarts;
        Profile = LoadProfile();
        ApplyUiScale();
        _notice = null;
    }

    private void OnTick(World world)
    {
        if (!ReferenceEquals(world, _world))
        {
            Resync(world);
            return;
        }

        var c = Court;
        if (c.Breaks != _breaks)
        {
            Play(SoundEvent.Brick);
        }
        else if (c.Hits != _hits)
        {
            Play(SoundEvent.Tough);
        }

        if (c.Clinks != _clinks)
        {
            Play(SoundEvent.Clink);
        }

        if (c.PaddleBounces != _paddles)
        {
            Play(SoundEvent.Paddle);
        }
        else if (c.WallBounces != _walls)
        {
            Play(SoundEvent.Wall);
        }

        if (c.Catches != _catches)
        {
            Play(SoundEvent.Catch);
        }

        if (c.Drops != _drops)
        {
            Play(SoundEvent.Lost);
        }

        if (c.Clears != _clears)
        {
            Play(SoundEvent.Clear);
        }

        if (_phase == CourtPhase.Serve && c.Phase == CourtPhase.Playing)
        {
            Play(SoundEvent.Launch);
        }

        if (_phase != CourtPhase.Over && c.Phase == CourtPhase.Over)
        {
            ScoreHandled = false;
            NameDraft = string.Empty;
            Play(SoundEvent.Over);
        }

        _phase = c.Phase;
        _hits = c.Hits;
        _breaks = c.Breaks;
        _clinks = c.Clinks;
        _walls = c.WallBounces;
        _paddles = c.PaddleBounces;
        _catches = c.Catches;
        _drops = c.Drops;
        _clears = c.Clears;
    }

    // ---- input that is not about a screen's own buttons --------------------------------------------------------------

    /// <summary>
    /// Keys and buttons nothing on screen took. The paddle itself is steered in <see cref="CourtView"/> by polling, because holding a key is
    /// a state and not an event; launching and pausing are one-shot actions, so they arrive here as events.
    /// </summary>
    public void HandleInput(in PlatformEvent e)
    {
        if (e.Kind == PlatformEventKind.AppLifecycle)
        {
            if (e.Lifecycle == AppLifecycleState.Paused)
            {
                TogglePauseOn();
            }

            return;
        }

        if (Game.CurrentScreen != "play")
        {
            return;
        }

        if (e.Kind == PlatformEventKind.KeyDown && !e.IsRepeat)
        {
            switch (e.Key)
            {
                case Key.Space or Key.Enter or Key.ArrowUp or Key.W:
                    Launch();
                    break;
                case Key.P:
                    TogglePause();
                    break;
            }
        }
        else if (e.Kind == PlatformEventKind.GamepadButtonDown)
        {
            switch (e.GamepadButton)
            {
                case GamepadButton.South or GamepadButton.North:
                    Launch();
                    break;
                case GamepadButton.Start:
                    TogglePause();
                    break;
            }
        }
    }

    // Leaving the foreground pauses a game in progress, so the ball is where it was when the player comes back.
    private void TogglePauseOn()
    {
        if (Game.CurrentScreen == "play" && Court.InPlay && !Court.Paused)
        {
            Court.Send.Pause(true);
        }
    }
}
