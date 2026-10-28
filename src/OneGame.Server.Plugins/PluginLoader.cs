using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Loader;

namespace OneGame.Server.Plugins;

/// <summary>
/// Loads server plugins into collectible <see cref="AssemblyLoadContext"/>s. The plugin's dll is read into memory first, so the file on
/// disk is free to be rebuilt while the old copy runs (a hot reload loads the new bytes). Engine assemblies (<c>OneGame.*</c>) and the
/// framework are shared with the host, so a plugin's entity types are the same <c>Entity</c> the host's worlds work with; everything
/// else is loaded from the plugin's own folder.
/// <para>
/// This is the only assembly that uses runtime reflection; it is CoreCLR-only and is never part of a NativeAOT publish.
/// </para>
/// </summary>
/// <example>
/// <code>
/// var host = new ServerHost(ServerConfig.Load("server.yaml"), setup: null, loader: new PluginLoader());
/// host.Start();                                    // loads the plugins the configuration lists
/// host.ReloadPlugin("pulse");                      // after rebuilding pulse's dll
/// </code>
/// </example>
public sealed class PluginLoader : IPluginLoader
{
    private const long MaxAssemblyBytes = 256L * 1024 * 1024;

    /// <inheritdoc />
    public PluginImage Load(string name, string path)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentException.ThrowIfNullOrEmpty(path);
        var file = Directory.Exists(path) ? Path.Combine(path, name + ".dll") : path;
        if (!File.Exists(file))
        {
            throw new PluginException("not_found", Directory.Exists(path)
                ? $"Plugin '{name}' has no {name}.dll in {path}. Build the plugin there, or point 'path' at the dll."
                : $"Plugin file {path} does not exist.");
        }

        var length = new FileInfo(file).Length;
        if (length > MaxAssemblyBytes)
        {
            throw new PluginException("load_failed", $"{file} is larger than {MaxAssemblyBytes >> 20} MiB.");
        }

        var context = new PluginLoadContext(name, Path.GetDirectoryName(Path.GetFullPath(file))!);
        try
        {
            return Open(name, file, context);
        }
        catch (PluginException)
        {
            context.Unload();
            throw;
        }
        catch (Exception ex) when (ex is BadImageFormatException or IOException or FileLoadException or TypeLoadException or MissingMethodException or TargetInvocationException or InvalidCastException)
        {
            context.Unload();
            throw new PluginException("load_failed", $"Plugin '{name}' could not be loaded from {file}: {ex.GetType().Name}: {ex.Message}");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static LoadedImage Open(string name, string file, PluginLoadContext context)
    {
        var assembly = context.LoadBytes(File.ReadAllBytes(file));

        // Read the declared API version from the assembly's metadata before constructing anything of the plugin's.
        var (entry, api) = Inspect(assembly, name, file);
        if (api < PluginApi.MinimumVersion || api > PluginApi.Version)
        {
            throw new PluginException("api_mismatch", $"Plugin '{name}' was built against plugin API {api}; this server speaks {PluginApi.MinimumVersion}..{PluginApi.Version}. Rebuild the plugin against the server's OneGame.Server assembly.");
        }

        if (!typeof(ServerPlugin).IsAssignableFrom(entry))
        {
            throw new PluginException("no_entry", $"Plugin '{name}': {entry.FullName} does not derive from ServerPlugin.");
        }

        var plugin = (ServerPlugin?)Activator.CreateInstance(entry)
            ?? throw new PluginException("no_entry", $"Plugin '{name}': {entry.FullName} could not be created.");
        var build = assembly.GetName().Version + "+" + assembly.ManifestModule.ModuleVersionId.ToString("N")[..8];
        return new LoadedImage(plugin, context, build, api);
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (Type Entry, int Api) Inspect(Assembly assembly, string name, string file)
    {
        foreach (var data in assembly.GetCustomAttributesData())
        {
            if (data.AttributeType.FullName != typeof(ServerPluginAttribute).FullName)
            {
                continue;
            }

            if (data.ConstructorArguments.Count >= 2
                && data.ConstructorArguments[0].Value is Type entry
                && data.ConstructorArguments[1].Value is int api)
            {
                return (entry, api);
            }
        }

        throw new PluginException("no_entry", $"Plugin '{name}' ({file}) has no [assembly: ServerPlugin(typeof(...))] attribute naming its entry class.");
    }
}

internal sealed class PluginLoadContext : AssemblyLoadContext
{
    private static readonly string[] Shared = ["OneGame.", "System.", "Microsoft.", "netstandard", "mscorlib"];
    private readonly string _folder;

    public PluginLoadContext(string name, string folder)
        : base("plugin:" + name, isCollectible: true)
    {
        _folder = folder;
    }

    public Assembly LoadBytes(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes, writable: false);
        return LoadFromStream(stream);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        var simple = assemblyName.Name;
        if (simple is null)
        {
            return null;
        }

        foreach (var prefix in Shared)
        {
            if (simple.StartsWith(prefix, StringComparison.Ordinal) || simple == prefix)
            {
                return null;
            }
        }

        var file = Path.Combine(_folder, simple + ".dll");
        return File.Exists(file) ? LoadBytes(File.ReadAllBytes(file)) : null;
    }
}

internal sealed class LoadedImage : PluginImage
{
    private ServerPlugin? _plugin;
    private PluginLoadContext? _context;
    private readonly WeakReference _weak;

    public LoadedImage(ServerPlugin plugin, PluginLoadContext context, string build, int api)
    {
        _plugin = plugin;
        _context = context;
        _weak = new WeakReference(context, trackResurrection: true);
        Build = build;
        ApiVersion = api;
    }

    public override ServerPlugin Plugin => _plugin ?? throw new ObjectDisposedException("plugin", "The plugin was released.");

    public override string Build { get; }

    public override int ApiVersion { get; }

    public override bool Owns(Exception exception)
    {
        var context = _context;
        for (var ex = exception; ex is not null; ex = ex.InnerException)
        {
            if (FramesIn(ex, context))
            {
                return true;
            }

            if (ex is AggregateException agg)
            {
                foreach (var inner in agg.InnerExceptions)
                {
                    if (Owns(inner))
                    {
                        return true;
                    }
                }
            }
        }

        return false;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool FramesIn(Exception ex, AssemblyLoadContext? context)
    {
        if (context is null)
        {
            return false;
        }

        foreach (var frame in new StackTrace(ex, false).GetFrames())
        {
            var assembly = frame.GetMethod()?.DeclaringType?.Assembly;
            if (assembly is not null && AssemblyLoadContext.GetLoadContext(assembly) == context)
            {
                return true;
            }
        }

        return false;
    }

    public override void Release()
    {
        _plugin = null;
        var context = _context;
        _context = null;
        context?.Unload();
    }

    public override bool WaitUntilCollected()
    {
        for (var i = 0; i < 20 && _weak.IsAlive; i++)
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        return !_weak.IsAlive;
    }
}
