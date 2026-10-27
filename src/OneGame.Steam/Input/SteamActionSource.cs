using OneGame.Platform;

namespace OneGame.Steam.Input;

/// <summary>
/// Feeds Steam Input into the engine's <see cref="ActionMap"/>: an action of the map and a Steam action of the same name and kind (button, vector, one-axis) are one action, so game code reads
/// <c>actions.Pressed("jump")</c> whether the player uses a keyboard, a pad or a Steam controller configured in Steam's own binding screen. Declare the actions once in the map and
/// build the Steam declaration (and its actions file) from it with <see cref="CreateActions"/>.
/// Values are read when the map updates, from whatever <see cref="SteamInputService.Update"/> last saw, so keep calling <see cref="SteamInputBridge.Update"/> (or the service) once a frame in <c>OnEachFrame</c>.
/// Steam keeps one action set active at a time (the one that lists the open screen, else the first), so with Steam the contexts that matter are the ones named in <c>OnScreens</c>.
/// </summary>
public sealed class SteamActionSource : IDisposable
{
    private readonly SteamInputService _service;
    private readonly ActionMap _map;
    private readonly ActionSource _source;
    private readonly List<(InputAction Action, SteamActionKind Kind)> _pairs = [];
    private bool _disposed;

    /// <summary>Attaches to a map.</summary>
    /// <param name="service">The Steam Input service (real or the harness stand-in).</param>
    /// <param name="map">The game's actions. Only actions whose name and kind match a Steam action are fed.</param>
    public SteamActionSource(SteamInputService service, ActionMap map)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
        _map = map ?? throw new ArgumentNullException(nameof(map));
        _source = map.CreateSource("steam");
        foreach (var (name, kind, _) in service.Actions.AllActions())
        {
            if (map.TryGet(name, out var a) && ((a.Kind == ActionKind.Button && kind == SteamActionKind.Digital) || (a.Kind == ActionKind.Vector && kind == SteamActionKind.Stick) || (a.Kind == ActionKind.Axis && kind == SteamActionKind.Trigger)))
            {
                _pairs.Add((a, kind));
            }
        }

        map.Updating += Poll;
    }

    /// <summary>How many actions of the map are fed from Steam.</summary>
    public int FedActions => _pairs.Count;

    /// <summary>
    /// Builds the Steam declaration from the map: one action set per context (named after it, with the screens it lists), a button action per button, a joystick-move stick action per vector and a one-axis action per axis.
    /// The result writes the in-game actions file like any <see cref="SteamInputActions"/>.
    /// </summary>
    /// <param name="map">The game's actions.</param>
    /// <param name="majorRevision">The file's major revision; bump it when the actions change.</param>
    /// <returns>The declaration.</returns>
    public static SteamInputActions CreateActions(ActionMap map, int majorRevision = 0)
    {
        ArgumentNullException.ThrowIfNull(map);
        var actions = new SteamInputActions { MajorRevision = majorRevision };
        foreach (var c in map.Contexts)
        {
            actions.Set(c.Name, c.Name, s =>
            {
                foreach (var a in map.Actions)
                {
                    if (a.Context != c.Name)
                    {
                        continue;
                    }

                    switch (a.Kind)
                    {
                        case ActionKind.Button:
                            s.Button(a.Name, a.Title);
                            break;
                        case ActionKind.Vector:
                            s.Stick(a.Name, a.Title);
                            break;
                        default:
                            s.Trigger(a.Name, a.Title);
                            break;
                    }
                }

                s.OnScreens([.. c.Screens]);
            });
        }

        return actions;
    }

    private void Poll()
    {
        if (_disposed || !_service.IsEnabled)
        {
            return;
        }

        foreach (var (a, kind) in _pairs)
        {
            switch (kind)
            {
                case SteamActionKind.Digital:
                    _source.Set(a.Name, _service.IsDown(a.Name));
                    break;
                case SteamActionKind.Stick:
                    _service.GetAnalog(a.Name, out var x, out var y);
                    // Steam reports up as positive; actions run down-positive like the screen.
                    _source.Set(a.Name, Math.Clamp(x, -1f, 1f), Math.Clamp(-y, -1f, 1f));
                    break;
                default:
                    _service.GetAnalog(a.Name, out var v, out _);
                    _source.Set(a.Name, Math.Clamp(v, -1f, 1f));
                    break;
            }
        }
    }

    /// <summary>Detaches from the map and lets go of every action it held.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _map.Updating -= Poll;
        _source.Clear();
    }
}
