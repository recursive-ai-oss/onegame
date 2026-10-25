using OneGame.Core;
using OneGame.Host;
using OneGame.Platform;
using OneGame.UI;
using OneGame.UI.Kit;

namespace Bricks.Views;

/// <summary>
/// The play screen: a bar with the score, level and lives, the court with its 168 brick cells, balls, capsules and paddle, and the pause and
/// game-over dialogs. Everything on the court is an ordinary UI element placed from the court's state every frame, so the 2D renderer
/// draws it with the rest of the UI. Between simulation ticks the balls and the paddle are drawn part of the way from where they were to
/// where they are (<see cref="GameInstance.FrameAlpha"/>), so a 144 Hz screen shows smooth motion from a 60 Hz simulation; stepped runs and
/// captures always show whole ticks. The class only reads the court and sends commands.
/// </summary>
[View]
internal sealed class CourtView
{
    internal const float HudUnits = 44;
    private const int Cells = Rules.Columns * Rules.Rows;
    private const int MaxGhosts = 24;
    private const int GhostFrames = 14;
    private const int FlashFrames = 6;

    private readonly Session _s;
    private readonly GameInstance _game;
    private readonly ushort[] _cells = new ushort[Cells];
    private readonly int[] _flash = new int[Cells];
    private readonly float[] _prevX = new float[Rules.MaxBalls];
    private readonly float[] _prevY = new float[Rules.MaxBalls];
    private readonly float[] _curX = new float[Rules.MaxBalls];
    private readonly float[] _curY = new float[Rules.MaxBalls];
    private readonly (float X, float Y, int Age, Color Tint)[] _ghosts = new (float, float, int, Color)[MaxGhosts];

    private Element _board = null!;
    private float _k = 1;
    private float _ox;
    private float _oy;
    private int _ballCount;
    private float _prevPaddle;
    private float _curPaddle;
    private float _half = Rules.PaddleWidth / 2f;
    private World? _world;
    private long _tick = -1;
    private long _lastFrame = -1;
    private int _level;
    private CourtPhase _phase;
    private float _lastPointerX = float.NaN;
    private float _lastPointerY = float.NaN;
    private float _lastSteer = float.NaN;
    private int _lastPush;

    private CourtView(GameInstance game)
    {
        _game = game;
        _s = Session.For(game);
    }

    private Court C => _s.Court;

    /// <summary>Builds the play screen.</summary>
    public static Element Build(GameInstance game) => new CourtView(game).Make();

    private Element Make()
    {
        _board = new Element("Board").Style(st => st.Grow(1).Shrink(1).MinHeight(Length.Px(0)).Overflow(OverflowMode.Hidden));
        _board.OnUpdate(_ => Update());
        _board.On(UiEventKind.PointerDown, (_, e) =>
        {
            if (e.Button != 0)
            {
                return false;
            }

            _s.Launch();
            return true;
        });

        var court = Ui.Box().Class("court").Style(st => st.Position(PositionMode.Absolute).PointerEvents(PointerEvents.None))
            .BindStyle(st => st.Left(_ox).Top(_oy).Width(Rules.Width * _k).Height(Rules.Height * _k));
        for (var i = 0; i < Cells; i++)
        {
            court.Add(MakeBrick(i));
        }

        for (var i = 0; i < MaxGhosts; i++)
        {
            court.Add(MakeGhost(i));
        }

        for (var i = 0; i < Rules.MaxCapsules; i++)
        {
            court.Add(MakeCapsule(i));
        }

        court.Add(MakePaddle());
        for (var i = 0; i < Rules.MaxBalls; i++)
        {
            court.Add(MakeBall(i));
        }

        _board.Add(court);
        _board.Add(MakeBanner());

        var root = Ui.Column(MakeBar(), _board, MakePause(), MakeOver())
            .Class("screen")
            .Named("play-screen")
            .Width(Length.Percent(100))
            .Height(Length.Percent(100));
        root.On(UiEventKind.Cancel, (_, _) =>
        {
            _s.TogglePause();
            return true;
        });
        return _s.Kit.AttachTo(root);
    }

    // ---- the bar and the dialogs -------------------------------------------------------------------------------------

    private Element MakeBar()
    {
        var hearts = new Element[5];
        for (var i = 0; i < hearts.Length; i++)
        {
            var n = i;
            hearts[i] = Ui.Image("heart", 32, 32).Size(Length.Ui(20), Length.Ui(20)).BindVisible(() => C.Lives > n);
        }

        return Ui.Row(
            Ui.Label("Score").Class("dim"),
            Ui.Label(string.Empty).Class("stat").Named("score").BindText(() => C.Score, v => v.ToString(CultureInfo.InvariantCulture)),
            Ui.Label("Level").Class("dim"),
            Ui.Label(string.Empty).Class("stat").Named("level").BindText(() => C.Level, v => v.ToString(CultureInfo.InvariantCulture)),
            Ui.Row(hearts).Named("lives").Gap(Length.Ui(3)),
            Ui.Spacer(),
            // Not focusable: Space and Enter launch the ball, so nothing on the play screen may take the keyboard focus (touch, mouse and P pause).
            UiKit.Button("Pause", _s.TogglePause, "pause-button").Focusable(false))
            .Class("bar")
            .Height(Length.Ui(HudUnits))
            .Shrink(0);
    }

    private DialogView MakePause()
    {
        var dialog = UiKit.Dialog("Paused", Ui.Label("Take your time.").Class("dim"), "pause", onCancel: _s.TogglePause);
        dialog.AddButton("Resume", _s.TogglePause, ButtonKind.Primary, isDefault: true, id: "resume");
        dialog.AddButton("Settings", () => _s.Open("settings"), id: "pause-settings");
        dialog.AddButton("Quit to title", _s.ToTitle, id: "pause-title");
        dialog.Buttons.Column().Gap(Length.Ui(8));
        return dialog.BindOpen(() => C.Paused && C.InPlay);
    }

    private DialogView MakeOver()
    {
        var entry = UiKit.TextInput(() => _s.NameDraft, v => _s.NameDraft = v, "Your name", maxLength: Profile.NameLength, onSubmit: _ => _s.SaveScore(_s.NameDraft), id: "name");
        var save = UiKit.Button("Save score", () => _s.SaveScore(_s.NameDraft), "save-score", ButtonKind.Primary).BindVisible(() => _s.CanEnterScore);
        var body = Ui.Column(
            Ui.Label(string.Empty).Class("heading").Named("over-reason").BindText(() => C.Victory, v => v ? "Every level cleared!" : "Out of lives"),
            Ui.Label(string.Empty).Named("over-score").BindText(() => C.Score, v => "Score " + v.ToString(CultureInfo.InvariantCulture) + " on level " + C.Level.ToString(CultureInfo.InvariantCulture)),
            Ui.Label("You made the table. Enter a name:").Class("best").BindVisible(() => _s.CanEnterScore),
            Ui.Row(entry.Grow().BindVisible(() => _s.CanEnterScore), save).Gap(Length.Ui(8)),
            Ui.Label(string.Empty).Class("dim").Named("over-notice").BindText(() => _s.Notice ?? string.Empty))
            .Gap(Length.Ui(8))
            .Width(Length.Ui(320));
        var dialog = UiKit.Dialog("Game over", body, "over", onCancel: _s.ToTitle);
        dialog.AddButton("Play again", _s.StartGame, ButtonKind.Primary, isDefault: true, id: "again");
        dialog.AddButton("High scores", () => _s.Open("scores"), id: "over-scores");
        dialog.AddButton("Quit to title", _s.ToTitle, id: "over-title");
        dialog.Buttons.Column().Gap(Length.Ui(8));
        return dialog.BindOpen(() => C.Phase == CourtPhase.Over);
    }

    private Element MakeBanner() =>
        Ui.Column(
            Ui.Label(string.Empty).Class("banner").Named("banner").BindText(() => C.Phase == CourtPhase.Cleared ? 1 : C.Phase == CourtPhase.Serve ? 2 : 0, BannerText),
            Ui.Label(string.Empty).Class("dim").Named("hint").BindText(() => C.Phase == CourtPhase.Serve && !C.Paused, v => v ? "Tap, click or press Space to launch" : string.Empty))
            .Align(Align.Center)
            .Gap(Length.Ui(6))
            .Style(st => st.Position(PositionMode.Absolute).PointerEvents(PointerEvents.None).TextAlign(TextAlignment.Center))
            .BindStyle(st => st.Left(_ox).Width(Rules.Width * _k).Top(_oy + (Rules.Height * 0.6f * _k)));

    private string BannerText(int kind) => kind switch
    {
        1 => "Level cleared!",
        2 => "Level " + C.Level.ToString(CultureInfo.InvariantCulture) + ": " + Levels.Get(Math.Max(1, C.Level)).Name,
        _ => string.Empty,
    };

    // ---- the pieces of the court -------------------------------------------------------------------------------------

    private Element MakeBrick(int cell)
    {
        var col = cell % Rules.Columns;
        var row = cell / Rules.Columns;
        return new Element("Brick")
            .Style(st => st.Position(PositionMode.Absolute))
            .BindVisible(() => Brick.Hits(_cells[cell]) > 0)
            .BindStyle(st =>
            {
                var v = _cells[cell];
                var solid = Brick.Hits(v) == Brick.Unbreakable;
                st.Left(col * Rules.BrickWidth * _k).Top((Rules.GridTop + (row * Rules.BrickHeight)) * _k)
                    .Width(Rules.BrickWidth * _k).Height(Rules.BrickHeight * _k)
                    .BackgroundImage(solid ? "brick-solid" : "brick")
                    .BackgroundTint(BrickColor(v, _flash[cell]));
            });
    }

    private Element MakeGhost(int i) =>
        new Element("Ghost")
            .Style(st => st.Position(PositionMode.Absolute).BackgroundImage("brick"))
            .BindVisible(() => _ghosts[i].Age is > 0 and <= GhostFrames)
            .BindStyle(st =>
            {
                var g = _ghosts[i];
                var grow = 1 + (g.Age * 0.05f);
                var w = Rules.BrickWidth * _k * grow;
                var h = Rules.BrickHeight * _k * grow;
                st.Left(g.X - (w / 2)).Top(g.Y - (h / 2)).Width(w).Height(h).Opacity(Math.Max(0f, 1f - (g.Age / (float)GhostFrames))).BackgroundTint(g.Tint);
            });

    private Element MakeCapsule(int i) =>
        new Element("Capsule")
            .Style(st => st.Position(PositionMode.Absolute))
            .BindVisible(() => i < C.Capsules.Count)
            .BindStyle(st =>
            {
                if (i >= C.Capsules.Count)
                {
                    return;
                }

                var c = C.Capsules[i];
                st.Left((c.X.ToSingle() - 14) * _k).Top((c.Y.ToSingle() - 7) * _k).Width(28 * _k).Height(14 * _k)
                    .BackgroundImage(c.Kind == CapsuleKind.Wide ? "capsule-wide" : "capsule-multi");
            });

    private Element MakePaddle() =>
        new Element("Paddle")
            .Named("paddle")
            .Style(st => st.Position(PositionMode.Absolute).BackgroundImage("paddle"))
            .BindVisible(() => C.InPlay)
            .BindStyle(st =>
            {
                var x = Lerp(_prevPaddle, _curPaddle, Alpha);
                st.Left((x - _half) * _k).Top((Rules.PaddleY - (Rules.PaddleHeight / 2f)) * _k).Width(_half * 2 * _k).Height(Rules.PaddleHeight * _k);
            });

    private Element MakeBall(int i) =>
        new Element("Ball")
            .Named("ball-" + i.ToString(CultureInfo.InvariantCulture))
            .Style(st => st.Position(PositionMode.Absolute).BackgroundImage("ball"))
            .BindVisible(() => i < _ballCount)
            .BindStyle(st =>
            {
                var x = Lerp(_prevX[i], _curX[i], Alpha);
                var y = Lerp(_prevY[i], _curY[i], Alpha);
                st.Left((x - Rules.BallRadius) * _k).Top((y - Rules.BallRadius) * _k).Width(Rules.BallRadius * 2 * _k).Height(Rules.BallRadius * 2 * _k);
            });

    private float Alpha => _game.FrameAlpha.ToSingle();

    private static float Lerp(float a, float b, float t) => a + ((b - a) * t);

    private static Color BrickColor(ushort v, int flash)
    {
        var hits = Brick.Hits(v);
        var tint = Brick.Tint(v);
        var color = tint switch
        {
            >= 1 and <= 6 => Theme.Tints[tint],
            7 => Theme.Steel[Math.Clamp(hits, 1, 3)],
            _ => Theme.Solid,
        };
        return flash > 0 ? KitContrast.Mix(color, Color.White, 0.6 * flash / FlashFrames) : color;
    }

    // ---- per-frame update --------------------------------------------------------------------------------------------

    private void Update()
    {
        var ui = _game.Ui;
        var margin = 10 * ui.Scale;
        var w = ui.Width;
        var h = Math.Max(1, ui.Height - (HudUnits * ui.Scale));
        _k = Math.Max(0.05f, Math.Min((w - (2 * margin)) / Rules.Width, (h - (2 * margin)) / Rules.Height));
        _ox = (w - (Rules.Width * _k)) / 2;
        _oy = (h - (Rules.Height * _k)) / 2;

        var court = C;
        var world = _game.World;
        var fresh = !ReferenceEquals(world, _world) || court.Level != _level || court.Phase is CourtPhase.Idle or CourtPhase.Serve || _phase == CourtPhase.Idle;
        _world = world;
        var newTick = world.Tick != _tick;
        var snap = fresh || _tick < 0;
        if (newTick)
        {
            _tick = world.Tick;
            for (var i = 0; i < Rules.MaxBalls; i++)
            {
                _prevX[i] = _curX[i];
                _prevY[i] = _curY[i];
            }

            _prevPaddle = _curPaddle;
        }

        // Where things are now (the balls on the court, the paddle).
        _ballCount = Math.Min(court.Balls.Count, Rules.MaxBalls);
        for (var i = 0; i < _ballCount; i++)
        {
            _curX[i] = court.Balls[i].X.ToSingle();
            _curY[i] = court.Balls[i].Y.ToSingle();
        }

        _curPaddle = court.PaddleX.ToSingle();
        var wantHalf = court.PaddleHalf.ToSingle();
        _half = snap ? wantHalf : _half + Math.Clamp(wantHalf - _half, -3f, 3f);
        if (snap)
        {
            for (var i = 0; i < Rules.MaxBalls; i++)
            {
                _prevX[i] = _curX[i];
                _prevY[i] = _curY[i];
            }

            _prevPaddle = _curPaddle;
        }

        UpdateBricks(court, snap);
        _level = court.Level;
        _phase = court.Phase;
        Poll(court);
    }

    private void UpdateBricks(Court court, bool snap)
    {
        var advance = _game.Frame != _lastFrame;
        _lastFrame = _game.Frame;
        var have = court.Bricks.Count == Cells;
        for (var i = 0; i < Cells; i++)
        {
            var now = have ? court.Bricks[i] : (ushort)0;
            var before = _cells[i];
            if (now != before)
            {
                if (!snap && Brick.Hits(now) == 0 && Brick.Hits(before) > 0)
                {
                    SpawnGhost(i, before);
                }
                else if (!snap && Brick.Hits(now) is > 0 and < Brick.Unbreakable && Brick.Hits(now) < Brick.Hits(before))
                {
                    _flash[i] = FlashFrames;
                }

                _cells[i] = now;
            }
            else if (advance && _flash[i] > 0)
            {
                _flash[i]--;
            }

            if (snap)
            {
                _flash[i] = 0;
            }
        }

        for (var g = 0; g < MaxGhosts; g++)
        {
            if (snap)
            {
                _ghosts[g].Age = 0;
            }
            else if (advance && _ghosts[g].Age > 0)
            {
                _ghosts[g].Age = _ghosts[g].Age >= GhostFrames ? 0 : _ghosts[g].Age + 1;
            }
        }
    }

    private void SpawnGhost(int cell, ushort brick)
    {
        for (var g = 0; g < MaxGhosts; g++)
        {
            if (_ghosts[g].Age == 0)
            {
                var col = cell % Rules.Columns;
                var row = cell / Rules.Columns;
                _ghosts[g] = (
                    (col + 0.5f) * Rules.BrickWidth * _k,
                    (Rules.GridTop + ((row + 0.5f) * Rules.BrickHeight)) * _k,
                    1,
                    BrickColor(brick, 0));
                return;
            }
        }
    }

    // ---- steering ----------------------------------------------------------------------------------------------------

    // Held keys, sticks and the pointer are states, not events, so they are read each frame and a command is sent only when they change.
    // The platform's pointer follows a finger too, so touch steers the same way as a mouse.
    private void Poll(Court court)
    {
        if (court.Paused || court.Phase is not (CourtPhase.Serve or CourtPhase.Playing))
        {
            return;
        }

        var input = _game.Platform.Input;
        var px = input.PointerX;
        var py = input.PointerY;
        if (float.IsNaN(_lastPointerX))
        {
            _lastPointerX = px;
            _lastPointerY = py;
        }
        else if (px != _lastPointerX || py != _lastPointerY)
        {
            _lastPointerX = px;
            _lastPointerY = py;
            var x = Math.Clamp((px - _board.AbsoluteBounds.X - _ox) / _k, 0, Rules.Width);
            if (float.IsNaN(_lastSteer) || MathF.Abs(x - _lastSteer) >= 0.25f)
            {
                _lastSteer = x;
                court.Send.Steer(Fixed.FromDouble(x));
            }
        }

        var push = 0;
        if (input.IsKeyDown(Key.ArrowLeft) || input.IsKeyDown(Key.A))
        {
            push -= 100;
        }

        if (input.IsKeyDown(Key.ArrowRight) || input.IsKeyDown(Key.D))
        {
            push += 100;
        }

        // No call lists the connected pads, so the first few ids are asked for.
        for (var id = 0; id < 4; id++)
        {
            if (input.GetGamepad(id) is not { IsConnected: true } pad)
            {
                continue;
            }

            if (pad.IsDown(GamepadButton.DPadLeft))
            {
                push -= 100;
            }

            if (pad.IsDown(GamepadButton.DPadRight))
            {
                push += 100;
            }

            var axis = MathF.Abs(pad.GetAxis(GamepadAxis.LeftX)) >= MathF.Abs(pad.GetAxis(GamepadAxis.RightX)) ? pad.GetAxis(GamepadAxis.LeftX) : pad.GetAxis(GamepadAxis.RightX);
            if (MathF.Abs(axis) > 0.2f)
            {
                push += (int)MathF.Round(axis * 100);
            }
        }

        push = Math.Clamp(push, -100, 100);
        if (push != _lastPush)
        {
            _lastPush = push;
            court.Send.Push(push);
        }
    }
}
