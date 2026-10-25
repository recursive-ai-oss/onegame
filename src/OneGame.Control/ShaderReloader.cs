using OneGame.Host;
using OneShader;
using OneShader.Diagnostics;
using OneShader.Reflection;

namespace OneGame.Control;

/// <summary>One diagnostic of a failed shader compile.</summary>
/// <param name="File">The file the problem is in.</param>
/// <param name="Line">1-based line.</param>
/// <param name="Column">1-based column.</param>
/// <param name="Code">The diagnostic code, such as <c>OSH0300</c>.</param>
/// <param name="Message">What is wrong.</param>
public sealed record ShaderProblem(string File, int Line, int Column, string Code, string Message);

/// <summary>The state of one hot-reloadable shader.</summary>
/// <param name="Name">The registered name.</param>
/// <param name="Path">The shader file.</param>
/// <param name="State"><c>ok</c> (the running shader matches the file), <c>compile_error</c> or <c>incompatible</c> (the old shader keeps running).</param>
/// <param name="Reloads">How many times a new version was swapped in.</param>
/// <param name="Message">For a failed reload, the reason in one text, ready to show; otherwise null.</param>
/// <param name="Problems">The compile diagnostics, with file and line.</param>
public sealed record ShaderStatus(string Name, string Path, string State, int Reloads, string? Message, ShaderProblem[] Problems);

/// <summary>Result of the <c>shaders</c> operation.</summary>
/// <param name="Shaders">Every registered shader.</param>
/// <param name="Changed">The names whose file changed since the last check; each was recompiled.</param>
public sealed record ShadersResult(ShaderStatus[] Shaders, string[] Changed);

/// <summary>
/// Development-build shader hot reload. It watches the files of the game's <see cref="HotShaders"/>, recompiles a changed one, checks that
/// its interface still matches what the game was built with and swaps it in. A compile error or an interface change is reported and the
/// shader that was running keeps running.
/// </summary>
public sealed class ShaderReloader
{
    private readonly HotShaders _shaders;
    private readonly TextWriter _log;
    private readonly Dictionary<HotShader, Entry> _entries = [];
    private long _nextCheck;

    private sealed class Entry
    {
        public (string File, DateTime Stamp, long Length)[] Seen = [];
        public int Reloads;
        public string State = "ok";
        public string? Message;
        public ShaderProblem[] Problems = [];
    }

    /// <summary>Creates a reloader over a game's registrations.</summary>
    /// <param name="shaders">The game's <see cref="GameInstance.HotShaders"/>.</param>
    /// <param name="log">Where a failed reload is written as one line per problem. Defaults to standard error.</param>
    public ShaderReloader(HotShaders shaders, TextWriter? log = null)
    {
        _shaders = shaders ?? throw new ArgumentNullException(nameof(shaders));
        _log = log ?? Console.Error;
    }

    /// <summary>Checks the files at most every 250 ms. Cheap enough to call on every pass of a game loop.</summary>
    /// <returns>The names that were recompiled.</returns>
    public IReadOnlyList<string> Poll()
    {
        if (_shaders.Items.Count == 0)
        {
            return [];
        }

        var now = Environment.TickCount64;
        if (now < _nextCheck)
        {
            return [];
        }

        _nextCheck = now + 250;
        return Check();
    }

    /// <summary>Checks every registered file now and recompiles the ones that changed since the last check (all of them on the first check).</summary>
    /// <returns>The names that were recompiled.</returns>
    public IReadOnlyList<string> Check()
    {
        var changed = new List<string>();
        foreach (var shader in _shaders.Items.ToArray())
        {
            if (!_entries.TryGetValue(shader, out var entry))
            {
                // The file as it was when the game registered it is the baseline; later edits are changes.
                entry = new Entry { Seen = [(System.IO.Path.GetFullPath(shader.Path), shader.FileTimeUtc, shader.FileLength)] };
                _entries[shader] = entry;
            }

            if (!Differs(entry))
            {
                continue;
            }

            changed.Add(shader.Name);
            Reload(shader, entry);
        }

        return changed;
    }

    /// <summary>The current state of every registered shader.</summary>
    public ShaderStatus[] Status()
    {
        var list = new List<ShaderStatus>();
        foreach (var shader in _shaders.Items)
        {
            _entries.TryGetValue(shader, out var e);
            list.Add(new ShaderStatus(shader.Name, shader.Path, e?.State ?? "ok", e?.Reloads ?? 0, e?.Message, e?.Problems ?? []));
        }

        return [.. list];
    }

    private static (string, DateTime, long)[] Stamp(IEnumerable<string> files)
    {
        var list = new List<(string, DateTime, long)>();
        foreach (var f in files)
        {
            var info = new FileInfo(f);
            list.Add(info.Exists ? (f, info.LastWriteTimeUtc, info.Length) : (f, DateTime.MinValue, -1));
        }

        return [.. list];
    }

    private static bool Differs(Entry e)
    {
        foreach (var (file, stamp, length) in e.Seen)
        {
            var info = new FileInfo(file);
            var now = info.Exists ? (info.LastWriteTimeUtc, info.Length) : (DateTime.MinValue, -1L);
            if (now.Item1 != stamp || now.Item2 != length)
            {
                return true;
            }
        }

        return false;
    }

    private void Reload(HotShader shader, Entry entry)
    {
        var result = ShaderCompiler.CompileFile(shader.Path);

        // Watch the file and its includes even when the compile failed, so fixing an include triggers the next try.
        entry.Seen = Stamp(result.Files.Count > 0 ? result.Files : [System.IO.Path.GetFullPath(shader.Path)]);
        if (!result.Success)
        {
            var problems = result.Diagnostics
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .Select(d => new ShaderProblem(d.Span.Path, d.Span.Line, d.Span.Column, d.Code, d.Message))
                .ToArray();
            Fail(shader, entry, "compile_error", string.Join("; ", problems.Select(p => $"{p.File}({p.Line},{p.Column}): error {p.Code}: {p.Message}")), problems);
            return;
        }

        var report = InterfaceCompatibility.Compare(shader.Baseline, result.Reflection!);
        if (!report.IsCompatible)
        {
            Fail(shader, entry, "incompatible",
                "The shader's interface changed, so it cannot replace the running one: " + string.Join("; ", report.Differences.Select(d => d.Message)) +
                ". Rebuild the project to use it.",
                []);
            return;
        }

        var blob = result.Find(shader.EntryPoint, CompileTarget.SpirV);
        if (blob is null)
        {
            Fail(shader, entry, "incompatible", $"The file has no entry point '{shader.EntryPoint}' any more.", []);
            return;
        }

        shader.Apply(blob.Data.ToArray());
        entry.Reloads++;
        entry.State = "ok";
        entry.Message = null;
        entry.Problems = [];
        _log.WriteLine($"shader reload: {shader.Name} reloaded from {shader.Path}");
    }

    private void Fail(HotShader shader, Entry entry, string state, string message, ShaderProblem[] problems)
    {
        entry.State = state;
        entry.Message = message;
        entry.Problems = problems;
        _log.WriteLine($"shader reload: {shader.Name} kept its old version ({state}): {message}");
    }
}
