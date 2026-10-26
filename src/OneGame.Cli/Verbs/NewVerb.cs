using System.Reflection;
using System.Text.Json;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>Creates a game project that builds and runs on the host.</summary>
internal sealed class NewVerb : IVerb
{
    public string Name => "new";

    public string Summary => "Create a game: onegame new <name> [--server] [--dir <folder>] [--engine <checkout>]; --server adds a dedicated-server project. Without --engine or ONEGAME_ROOT the game builds from the RecursiveAI.OneGame.Sdk package.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        string? name = null, dir = null, engine = null;
        var withServer = false;
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
                case "--server":
                    withServer = true;
                    break;
                case var a when a.StartsWith("--", StringComparison.Ordinal):
                    throw new CliException(ExitCodes.Usage, "bad_args", $"Unknown option {a}. Usage: onegame new <name> [--server] [--dir <folder>] [--engine <checkout>].");
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
        var sdkVersion = Program.GetVersion();
        var target = Path.GetFullPath(dir ?? name);
        if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
        {
            throw new CliException(ExitCodes.RuntimeError, "exists", $"{target} already exists and is not empty.");
        }

        // PascalCase, so the assembly and namespace carry no underscores (CA1707).
        var cls = string.Concat(name.Split('-', '_').Where(p => p.Length > 0).Select(p => char.ToUpperInvariant(p[0]) + p[1..]));
        var files = new Dictionary<string, string>
        {
            [name + ".csproj"] = Templates.Csproj(root, sdkVersion, cls, withServer),
            ["Program.cs"] = Templates.Program(cls, cls),
            ["Game.cs"] = Templates.Game(cls, cls, name, withServer),
            ["scenarios/click-button.yaml"] = Templates.Scenario,
            [".gitignore"] = "bin/\nobj/\nartifacts/\ncaptures/\n",
            ["README.md"] = Templates.Readme(name),
        };
        if (withServer)
        {
            // The simulation lives in its own file so the server project can compile it without the game's screens.
            files["Sim.cs"] = Templates.Sim(cls);
            files[$"server/{name}-server.csproj"] = Templates.ServerCsproj(root, sdkVersion, cls);
            files["server/Program.cs"] = Templates.ServerProgram(cls);
            files["server/server.yaml"] = Templates.ServerConfig(name);
        }

        foreach (var (relative, text) in files)
        {
            var path = Path.Combine(target, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, text);
        }

        ExportAssets.WriteDefaultFont(target);
        var created = files.Keys.Concat(["fonts/NotoSans-Regular.ttf", "fonts/OFL.txt"]);
        string[] next = [$"cd {target}", "onegame build", "onegame run --headless --frames 60", "onegame test"];
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(new NewResult(true, name, target, [.. created.Order(StringComparer.Ordinal)], root ?? "", next), CliJson.Default.NewResult));
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

    /// <summary>Where the engine sources are: the option, the environment, the tool's own checkout, or where this build came from.
    /// Null when there are none: the game then builds from the RecursiveAI.OneGame.Sdk package on nuget.org.</summary>
    private static string? FindEngine(string? option)
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
        return baked is not null && File.Exists(Path.Combine(baked, "OneGame.slnx")) ? Path.GetFullPath(baked) : null;
    }
}

/// <summary>The files of a new game.</summary>
internal static class Templates
{
    public static string Csproj(string? engine, string sdkVersion, string id, bool withServer = false) => $"""
        <Project Sdk="{Sdk(engine, sdkVersion)}">

          <PropertyGroup>
        {Root(engine)}    <AssemblyName>{id}</AssemblyName>
            <RootNamespace>{id}</RootNamespace>
          </PropertyGroup>
        {Import(engine, "Game")}{ServerExclusion(withServer)}
        </Project>

        """;

    // A game inside an engine checkout builds against its projects; any other game uses the SDK package, which brings the same
    // target framework, rules, references and (in Debug builds only) development tools from nuget.org.
    private static string Sdk(string? engine, string sdkVersion) => engine is null ? $"RecursiveAI.OneGame.Sdk/{sdkVersion}" : "Microsoft.NET.Sdk";

    private static string Root(string? engine) => engine is null ? "" : $"    <!-- The onegame checkout this project builds against. -->\n    <OneGameRoot>{engine}</OneGameRoot>\n";

    private static string Import(string? engine, string kind) => engine is null
        ? kind == "Server" ? "\n  <!-- A dedicated server: the SDK imports the server settings instead of the game's. -->\n  <PropertyGroup>\n    <OneGameKind>Server</OneGameKind>\n  </PropertyGroup>\n" : ""
        : $"\n  <!-- Target framework, analyzers, references, and (in Debug builds only) the development tools: live control, scenarios, capture. -->\n  <Import Project=\"$(OneGameRoot)/build/{kind}.targets\" />\n";

    private static string ServerExclusion(bool withServer) => withServer
        ? "\n<!-- The server project in server/ has its own Program. -->\n<ItemGroup>\n  <Compile Remove=\"server/**\" />\n</ItemGroup>\n"
        : "";

    public static string ServerCsproj(string? engine, string sdkVersion, string id) => $"""
        <Project Sdk="{Sdk(engine, sdkVersion)}">

          <PropertyGroup>
        {Root(engine)}    <AssemblyName>{id}Server</AssemblyName>
            <RootNamespace>{id}</RootNamespace>
            <EnableDefaultCompileItems>false</EnableDefaultCompileItems>
          </PropertyGroup>
        {Import(engine, "Server")}
          <ItemGroup>
            <Compile Include="Program.cs" />
            <!-- The simulation the game and the server share. Keep view code out of these files. -->
            <Compile Include="../Sim.cs" Link="Sim.cs" />
          </ItemGroup>

        </Project>

        """;

    public static string ServerProgram(string id) => $$"""
        using {{id}};
        using OneGame.Server;
        using OneGame.Server.Live;

        // The whole server: listen, tick, serve live control. See server.yaml for the options (or pass them on the command line).
        return ServerApp.Run(
            new ServerSetup
            {
                RegisterEntities = OneGame.Generated.{{id}}Server.EntityTypes.Register,
                SeedWorld = (world, _) => world.Add(new Counter()),
            },
            args);

        """;

    public static string ServerConfig(string name) => $$"""
        name: {{name}}
        tickRate: 60
        scopes:
          - { id: 1, seed: 1 }
        listen:
          - { transport: udp, port: 7777 }
          - { transport: ws, port: 7778 }

        """;

    public static string Sim(string id) => $$"""
        using OneGame.Core;

        namespace {{id}};

        /// <summary>The state of the game: one synced number and a command that changes it.</summary>
        [Entity]
        public partial class Counter : Entity
        {
            [Synced] public partial int Count { get; private set; }

            [Command]
            public void Increment(int by) => Count += by;
        }

        """;

    public static string Program(string id, string cls) => $$"""
        using {{id}};
        using OneGame.Host;

        // A named class rather than top-level statements, so the Android entry point (onegame export --target android) can call Main.
        internal static class Program
        {
            internal static int Main(string[] args) => GameApp.Run(new {{cls}}Game(), args);
        }

        """;

    public static string Game(string id, string cls, string name, bool withServer = false) => withServer ? GameOnly(id, cls, name) : GameWithSim(id, cls, name);

    private static string GameWithSim(string id, string cls, string name) => $$"""
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

    // With a server project the entity lives in Sim.cs; this is the game without it.
    private static string GameOnly(string id, string cls, string name) => $$"""
        using OneGame.Core;
        using OneGame.Host;
        using OneGame.UI;

        namespace {{id}};

        /// <summary>The game: its entity types, its starting world and its screens. Run it against a server with --connect udp://host:7777.</summary>
        public sealed class {{cls}}Game : GameDefinition
        {
            public override string Name => "{{name}}";

            public override void RegisterEntities(EntityRegistry registry) => OneGame.Generated.{{id}}.EntityTypes.Register(registry);

            public override void Setup(World world) => world.Add(new Counter());

            public override void DefineScreens(ScreenSet screens) =>
                screens.Add("main", game => Ui.Column(
                    Ui.Label(() => game.World.Entities.Count > 0 ? "Count: " + CounterOf(game).Count : "connecting").Named("count"),
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

        Scenarios live in `scenarios/`. `fonts/` holds the game's fonts (Noto Sans Regular to start, SIL Open Font License; its licence sits beside it). `onegame export --target linux-x64 --json` packages a Release build to ship; see the engine's `docs/exporting.md`. Debug builds include the development tools (live control, scenarios, capture, replay); Release builds leave them out.
        """;
}
