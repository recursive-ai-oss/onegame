using System.IO;
using System.Text;

namespace OneGame.Core;

/// <summary>The files of one named key: the private key file and the one-line public key file beside it.</summary>
/// <param name="Name">The name the files are called after.</param>
/// <param name="PrivateFile">The key file (<c>name.key</c>); a secret.</param>
/// <param name="PublicFile">The public key file (<c>name.pub</c>), safe to share and to ship.</param>
/// <param name="PublicKey">The public key.</param>
/// <param name="Created">True when this call made the files, false when they already existed and were read.</param>
public sealed record KeyFiles(string Name, string PrivateFile, string PublicFile, PeerKey PublicKey, bool Created);

/// <summary>
/// Makes and finds key files. <c>onegame keys</c> is this class on the command line. Development tools use
/// <see cref="Ensure"/> with <see cref="DevelopmentFolder"/>, so a developer never has to make a key by hand, and a development key
/// stays in a per-user folder outside the game, where no export or commit picks it up.
/// </summary>
public static class KeyStore
{
    /// <summary>Makes <c>name.key</c> and <c>name.pub</c> in <paramref name="folder"/>.</summary>
    /// <param name="folder">Where to write; created if missing. A <c>.gitignore</c> that ignores <c>*.key</c> is added when the folder has none.</param>
    /// <param name="name">The file name without extension.</param>
    /// <param name="force">Replace existing files. Without it, an existing key is never overwritten.</param>
    /// <param name="development">Mark the key as a development key.</param>
    /// <exception cref="IOException">The key file exists and <paramref name="force"/> is false.</exception>
    public static KeyFiles Create(string folder, string name, bool force = false, bool development = false)
    {
        var files = Paths(folder, name);
        if (File.Exists(files.PrivateFile) && !force)
        {
            throw new IOException($"'{files.PrivateFile}' already exists; pass --force to replace it (clients that trust its public key will stop trusting this server).");
        }

        var pair = development ? NetKeyPair.GenerateDevelopment() : NetKeyPair.Generate();
        pair.Save(files.PrivateFile);
        File.WriteAllText(files.PublicFile, pair.PublicKey + "\n");
        var ignore = Path.Combine(folder, ".gitignore");
        if (!File.Exists(ignore))
        {
            File.WriteAllText(ignore, "# private keys are secrets\n*.key\n");
        }

        return files with { PublicKey = pair.PublicKey, Created = true };
    }

    /// <summary>Reads <c>name.key</c> in <paramref name="folder"/>, making it first when it is missing.</summary>
    /// <param name="folder">The key folder.</param>
    /// <param name="name">The file name without extension.</param>
    /// <param name="development">Mark a key made by this call as a development key.</param>
    public static KeyFiles Ensure(string folder, string name, bool development = false)
    {
        var files = Paths(folder, name);
        if (!File.Exists(files.PrivateFile))
        {
            return Create(folder, name, force: false, development);
        }

        var pair = NetKeyPair.Load(files.PrivateFile);
        if (!File.Exists(files.PublicFile))
        {
            File.WriteAllText(files.PublicFile, pair.PublicKey + "\n");
        }

        return files with { PublicKey = pair.PublicKey, Created = false };
    }

    /// <summary>
    /// The per-user folder development keys live in for one project (or cluster): <c>ONEGAME_KEYS</c> if set, else the user's local
    /// application data. Never inside the project, so an export or a commit cannot pick a key up.
    /// </summary>
    /// <param name="scope">What the keys belong to, usually the project folder's full path; different scopes get different folders.</param>
    public static string DevelopmentFolder(string scope)
    {
        ArgumentNullException.ThrowIfNull(scope);
        var root = Environment.GetEnvironmentVariable("ONEGAME_KEYS");
        if (string.IsNullOrEmpty(root))
        {
            var local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            if (string.IsNullOrEmpty(local))
            {
                local = Path.GetTempPath();
            }

            root = Path.Combine(local, "onegame", "dev-keys");
        }

        Span<byte> hash = stackalloc byte[32];
        Sha256.Hash(Encoding.UTF8.GetBytes(scope), hash);
        return Path.Combine(root, Convert.ToHexStringLower(hash[..6]));
    }

    private static KeyFiles Paths(string folder, string name)
    {
        ArgumentException.ThrowIfNullOrEmpty(folder);
        ArgumentException.ThrowIfNullOrEmpty(name);
        if (name.AsSpan().IndexOfAny(['/', '\\', '.']) >= 0)
        {
            throw new ArgumentException("a key name is a plain word: no dots or slashes", nameof(name));
        }

        return new KeyFiles(name, Path.Combine(folder, name + ".key"), Path.Combine(folder, name + ".pub"), default, false);
    }
}
