using System.Text.Json;
using OneGame.Testing;

namespace OneGame.Cli.Tests;

public class CliTests
{
    private static (int Code, string Out) Run(params string[] args)
    {
        var output = new StringWriter();
        var code = Program.Run(args, output, new StringWriter());
        return (code, output.ToString());
    }

    [Fact]
    public void VersionJsonIsOneObject()
    {
        var (code, text) = Run("--version", "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal("onegame", doc.RootElement.GetProperty("name").GetString());
        Assert.Equal("1.10.0", doc.RootElement.GetProperty("version").GetString());
    }

    [Fact]
    public void HelpJsonListsHelp()
    {
        var (code, text) = Run("help", "--json");
        Assert.Equal(0, code);
        using var doc = JsonDocument.Parse(text);
        Assert.Equal("help", doc.RootElement.GetProperty("commands")[0].GetProperty("name").GetString());
    }

    [Fact]
    public void KeysMakesAKeyPairAndNeverOverwritesOne()
    {
        var folder = Path.Combine(Path.GetTempPath(), "onegame-keys-test-" + Guid.NewGuid().ToString("N"));
        try
        {
            var (code, text) = Run("keys", "--out", folder, "--name", "island", "--json");
            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            Assert.True(root.GetProperty("ok").GetBoolean());
            Assert.Equal("island", root.GetProperty("name").GetString());
            var publicKey = root.GetProperty("publicKey").GetString()!;
            Assert.Equal(43, publicKey.Length);
            Assert.Equal(publicKey + "\n", File.ReadAllText(root.GetProperty("publicFile").GetString()!));
            var privateText = File.ReadAllText(root.GetProperty("privateFile").GetString()!);
            Assert.Contains("private:", privateText, StringComparison.Ordinal);
            Assert.DoesNotContain(privateText.Split('\n').First(l => l.StartsWith("private:", StringComparison.Ordinal))[8..].Trim(), text, StringComparison.Ordinal); // the secret never prints
            Assert.Contains("*.key", File.ReadAllText(Path.Combine(folder, ".gitignore")), StringComparison.Ordinal);

            var (again, error) = Run("keys", "--out", folder, "--name", "island", "--json");
            Assert.NotEqual(0, again);
            Assert.Contains("key_exists", error, StringComparison.Ordinal);
            Assert.Equal(0, Run("keys", "--out", folder, "--name", "island", "--force", "--json").Code);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }

    [Fact]
    public void UnknownVerbFails()
    {
        var (code, text) = Run("nope", "--json");
        Assert.Equal(2, code);
        using var doc = JsonDocument.Parse(text);
        Assert.False(doc.RootElement.GetProperty("ok").GetBoolean());
        Assert.Equal("unknown_verb", doc.RootElement.GetProperty("error").GetProperty("code").GetString());
        Assert.Contains("nope", doc.RootElement.GetProperty("error").GetProperty("message").GetString(), StringComparison.Ordinal);
    }

    [Fact]
    public void ClusterKillEndsNamedNodesOfAnAttachedClusterAndPrintsJson()
    {
        var dir = Path.Combine(Path.GetTempPath(), "onegame-kill-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        // Two idle programs to stand in for nodes: Windows has no sleep, but every machine has ping.
        System.Diagnostics.Process Idle() => OperatingSystem.IsWindows()
            ? System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo("ping", "-n 120 127.0.0.1") { CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true })!
            : System.Diagnostics.Process.Start("sleep", "120")!;
        using var victim = Idle();
        using var bystander = Idle();
        try
        {
            NodeState Node(string name, int pid) => new(name, "server", pid, Path.Combine(dir, name + ".sock"), Path.Combine(dir, name + ".log"), [], null, null);
            var state = new ClusterState("kill-demo", dir, Environment.ProcessId, [Node("island", victim.Id), Node("other", bystander.Id)]);
            var file = Path.Combine(dir, "cluster.json");
            File.WriteAllText(file, JsonSerializer.Serialize(state, ClusterJson.Default.ClusterState));

            var (code, text) = Run("cluster", "kill", "island", "--cluster", file, "--json");
            Assert.Equal(0, code);
            using var doc = JsonDocument.Parse(text);
            Assert.True(doc.RootElement.GetProperty("ok").GetBoolean());
            var killed = doc.RootElement.GetProperty("result").GetProperty("nodes")[0];
            Assert.Equal("island", killed.GetProperty("name").GetString());
            Assert.Equal(victim.Id, killed.GetProperty("pid").GetInt32());
            Assert.False(killed.GetProperty("alive").GetBoolean());
            Assert.True(victim.WaitForExit(5000));
            Assert.False(bystander.HasExited); // only the named node

            var (missing, error) = Run("cluster", "kill", "nope", "--cluster", file, "--json");
            Assert.Equal(2, missing);
            Assert.Equal("no_node", JsonDocument.Parse(error).RootElement.GetProperty("error").GetProperty("code").GetString());
            Assert.Equal(2, Run("cluster", "kill", "--cluster", file, "--json").Code);
        }
        finally
        {
            bystander.Kill();
            Directory.Delete(dir, true);
        }
    }
}
