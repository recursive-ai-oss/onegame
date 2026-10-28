using OneGame.Core;
using OneGame.Host;

namespace OneGame.Cli.Verbs;

/// <summary>What <c>onegame keys</c> made.</summary>
/// <param name="Ok">True.</param>
/// <param name="Name">The key's name.</param>
/// <param name="PrivateFile">The secret key file.</param>
/// <param name="PublicFile">The public key file.</param>
/// <param name="PublicKey">The public key: what clients pin.</param>
internal sealed record KeysResult(bool Ok, string Name, string PrivateFile, string PublicFile, string PublicKey);

/// <summary>
/// <c>onegame keys</c> makes a key pair for a server (or a player). The private file is the developer's secret: keep it on the server
/// only, outside the repository. Clients are given the public key.
/// </summary>
internal sealed class KeysVerb : IVerb
{
    public string Name => "keys";

    public string Summary => "Make a key pair for a server or player: keys [--out DIR] [--name NAME] [--force]. Writes NAME.key (SECRET) and NAME.pub (share it); prints the public key.";

    public int Run(string[] args, bool json, TextWriter output, TextWriter error)
    {
        var folder = "keys";
        var name = "server";
        var force = false;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--out" when i + 1 < args.Length:
                    folder = args[++i];
                    break;
                case "--name" when i + 1 < args.Length:
                    name = args[++i];
                    break;
                case "--force":
                    force = true;
                    break;
                default:
                    throw new CliException(ExitCodes.Usage, "bad_args", $"Unexpected argument '{args[i]}'. Usage: onegame keys [--out DIR] [--name NAME] [--force] [--json].");
            }
        }

        KeyFiles files;
        try
        {
            files = KeyStore.Create(Path.GetFullPath(folder), name, force);
        }
        catch (IOException ex)
        {
            throw new CliException(ExitCodes.Failed, "key_exists", ex.Message);
        }
        catch (ArgumentException ex)
        {
            throw new CliException(ExitCodes.Usage, "bad_args", ex.Message);
        }

        var result = new KeysResult(true, files.Name, files.PrivateFile, files.PublicFile, files.PublicKey.ToString());
        if (json)
        {
            output.WriteLine(JsonSerializer.Serialize(result, CliJson.Default.KeysResult));
        }
        else
        {
            output.WriteLine($"public key: {result.PublicKey}");
            output.WriteLine($"private key file (a secret: keep it on the server, outside the repository): {result.PrivateFile}");
            output.WriteLine($"public key file (share this one): {result.PublicFile}");
        }

        return ExitCodes.Ok;
    }
}
