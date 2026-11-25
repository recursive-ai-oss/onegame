# Game options: a game's own command line

`GameApp.Run` takes the engine's options (`--headless`, `--frames`, `--connect` ...) and, in development builds, the development options (`--live`, `--scenario`, `--capture` ...). A game adds its own by declaring their names:

```csharp
public sealed class MyGame : GameDefinition
{
    public override IReadOnlyList<string> OptionNames => ["level", "fake-store"];
    // ...
}
```

- **Works in every build.** A declared option never needs the development tools: `mygame --level 3` runs the same in a release export, and beside `--live` or `--scenario` in a development build.
- **Read it from the instance.** `GameInstance.Args` is a `GameArgs`: `Has("fake-store")`, `Get("level")`, `GetInt("level", 1)`. Use it in view and host code (screen builders, `OnEachFrame`, the code that creates services). Sim code must not: the command line is not part of the world, the recording or the snapshot, so reading it would make a replay depend on how the game was started. Sim code has no way to reach it (a world does not hold the instance), and a static that carries it into an entity is outside API the sim analyzers warn about (OG0015, an error under warnings-as-errors).
- **Misspelt options still fail.** Only the declared names are exempt. `--scenaro` in a release build exits 5 `no_dev_tools`, and in a development build exits 2 `bad_args` ("Unknown option --scenaro"); both messages list the game's own options. A declared name that is also an engine or development option is that option's, not the game's. Names are given without dashes, spaces or `=`; anything else exits 2 `bad_game_option`.
- **Syntax.** `--name value`, `--name=value` or a bare `--name` (a flag). A bare flag directly before a non-dash word takes that word as its value, so put flags last or write `--flag=true`.
- **Where the line comes from.** Desktop and `onegame run -- --level 3` pass the arguments straight through (everything after `--` is the game's). Android: the launch intent's `onegame.args` extra (`-e onegame.args "'--level 3'"`; `onegame android run|test|live` forward their game arguments the same way). iOS: the process launch arguments (`simctl launch <device> <bundle> --level 3`). Browser: whatever arguments the page's host passes to `Main`; `GameApp.Run` reads them the same way.
- **`--json`.** A normal (non-development) run's summary has a `gameOptions` object with the declared options that were given, for scripts to check.
- A game that needs its options before an instance exists (to build its definition) reads the `args` of `Main` itself; it need not strip them out before calling `GameApp.Run`.
