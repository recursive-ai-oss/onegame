using System.Reflection;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Creates a game project that builds and runs on the host.</summary>
internal sealed class NewVerb : IVerb
{
    public string Name => "new";

    public string Summary => "Create a game: onegame new <name> [--dir <folder>] [--engine <checkout>].";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        string? name = null, dir = null, engine = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--dir" when i + 1 < args.Length:
                    dir = args[++i];
                    break;
                case "--engine" when i + 1 < args.Length:
                    engine = args[++i];
                    break;
                case var a when a.StartsWith("--", StringComparison.Ordinal):
                    throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {a}. Usage: onegame new <name> [--dir <folder>] [--engine <checkout>].");
                default:
                    name ??= args[i];
                    break;
            }
        }

        if (name is null || !IsName(name))
        {
            throw new CliException(ExitCodes.Usage, "bad_args", "Give the game a name of letters, digits, '-' and '_' that starts with a letter: onegame new demo.");
        }

        var root = FindEngine(engine);
        var target = Path.GetFullPath(dir ?? name);
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
        {
            throw new CliException(ExitCodes.RuntimeError, "exists", $"{target} already exists and is not empty.");
        }

        // PascalCase, so the assembly and namespace carry no underscores (CA1707).
        var cls = string.Concat(name.Split('-', '_').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
        var files = new Dictionary<string, string>
        {
            [name + ".csproj"] = Templates.Csproj(root, cls),
            ["Program.cs"] = Templates.Program(cls, cls),
            ["Game.cs"] = Templates.Game(cls, cls, name),
            ["scenarios/click-button.yaml"] = Templates.Scenario,
            [".gitignore"] = "bin/\nobj/\nartifacts/\ncaptures/\n",
            ["README.md"] = Templates.Readme(name),
        };
        foreach (var (relative, text) in files)
        {
            var path = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        string[] next = [$"cd {target}", "onegame build", "onegame run --headless --frames 60", "onegame test"];
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new NewResult(true, name, target, [.. files.Keys.Order(StringComparer.Ordinal)], root, next), CliJson.Default.NewResult));
        }
        else
        {
            output.WriteLine($"created {target}");
            foreach (var n in next)
            {
                output.WriteLine($"  {n}");
            }
        }

        return ExitCodes.Ok;
    }

    private static bool IsName(string name) =>
        name.Length is > 0 and <= 60 && char.IsAsciiLetter(name[0]) && name.All(c => char.IsAsciiLetterOrDigit(c) || c is '-' or '_');

    /// <summary>Where the engine sources are: the option, the environment, the tool's own checkout, or where this build came from.</summary>
    private static string FindEngine(string? option)
    {
        foreach (var candidate in new[] { option, Environment.GetEnvironmentVariable("ONEGAME_ROOT") })
        {
            if (!string.IsNullOrEmpty(candidate))
            {
                var full = Path.GetFullPath(candidate);
                return File.Exists(Path.Combine(full, "OneGame.slnx"))
                    ? full
                    : throw new CliException(ExitCodes.Usage, "no_engine", $"{full} is not an onegame checkout (no OneGame.slnx).");
            }
        }

        for (var d = new DirectoryInfo(AppContext.BaseDirectory); d is not null; d = d.Parent)
        {
            if (File.Exists(Path.Combine(d.FullName, "OneGame.slnx")))
            {
                return d.FullName;
            }
        }

        var baked = typeof(NewVerb).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().FirstOrDefault(m => m.Key == "OneGameRoot")?.Value;
        return baked is not null && File.Exists(Path.Combine(baked, "OneGame.slnx"))
            ? Path.GetFullPath(baked)
            : throw new CliException(ExitCodes.Usage, "no_engine", "Cannot find the onegame checkout. Pass --engine <folder> or set ONEGAME_ROOT.");
    }
}

/// <summary>The files of a new game.</summary>
internal static class Templates
{
    public static string Csproj(string engine, string id) => $"""
        <Project Sdk="Microsoft.NET.Sdk">

          <PropertyGroup>
            <!-- The onegame checkout this game builds against. -->
            <OneGameRoot>{engine}</OneGameRoot>
            <AssemblyName>{id}</AssemblyName>
            <RootNamespace>{id}</RootNamespace>
          </PropertyGroup>

          <!-- Target framework, analyzers, references, and (in Debug builds only) the development tools: live control, scenarios, capture. -->
          <Import Project="$(OneGameRoot)/build/Game.targets" />

        </Project>

        """;

    public static string Program(string id, string cls) => $$"""
        using {{id}};
        using OneGame.Host;

        return GameApp.Run(new {{cls}}Game(), args);

        """;

    public static string Game(string id, string cls, string name) => $$"""
        using OneGame.Core;
        using OneGame.Host;
        using OneGame.UI;

        namespace {{id}};

        /// <summary>The state of the game: one synced number and a command that changes it.</summary>
        [Entity]
        public partial class Counter : Entity
        {
            [Synced] public partial int Count { get; private set; }

            [Command]
            public void Increment(int by) => Count += by;
        }

        /// <summary>The game: its entity types, its starting world and its screens.</summary>
        public sealed class {{cls}}Game : GameDefinition
        {
            public override string Name => "{{name}}";

            public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.{{id}}.EntityTypes.Register(registry);

            public override void Setup(World world) => world.Add(new Counter());

            public override void DefineScreens(ScreenSet screens) =>
                screens.Add("main", game => Ui.Column(
                    Ui.Label(() => "Count: " + CounterOf(game).Count).Named("count"),
                    Ui.Button("Add one", () => CounterOf(game).Send.Increment(1)).Named("add").Padding(Length.Px(8)).Background(Color.Rgb(0x3060A0)))
                    .Padding(Length.Px(24)).Gap(Length.Px(12)).Background(Color.Rgb(0x203040)).TextColor(Color.White));

            // Read the world through the instance: it is replaced when a snapshot or fixture is loaded.
            private static Counter CounterOf(GameInstance game) => (Counter)game.World.Entities[0];
        }

        """;

    public const string Scenario = """
        name: click-button
        description: Pressing the button adds one to the counter.
        steps:
          - expect:
              ui: { id: count, text: "Count: 0" }
          - click: add
          - step: 1
          - click: add
          - step: 1
        expect:
          - state: { entity: Counter, property: Count, equals: 2 }
          - ui: { id: count, text: "Count: 2" }
        """;

    public static string Readme(string name) => $"""
        # {name}

        A onegame game.

        ```
        onegame build                              # build (Debug)
        onegame run --headless --frames 60 --json  # run without a window
        onegame run                                # run in a window
        onegame test --json                        # run scenarios/*.yaml
        onegame capture --size 720p                # captures/frame.png
        onegame mcp --project .                    # let an agent drive the game over MCP
        ```

        Scenarios live in `scenarios/`. Debug builds include the development tools (live control, scenarios, capture, replay); Release builds leave them out.
        """;
}
