namespace OneGame.Core.Tests;

public class KeyFileTests
{
    [Fact]
    public void ADevelopmentMarkSurvivesTheKeyFileAndARealKeyHasNone()
    {
        var dev = NetKeyPair.GenerateDevelopment();
        Assert.True(dev.IsDevelopment);
        var text = dev.ToFileText();
        Assert.Contains("# DEV KEY", text, StringComparison.Ordinal);
        var back = NetKeyPair.FromFileText(text);
        Assert.True(back.IsDevelopment);
        Assert.Equal(dev.PublicKey, back.PublicKey);

        var real = NetKeyPair.Generate();
        Assert.False(real.IsDevelopment);
        Assert.DoesNotContain("DEV KEY", real.ToFileText(), StringComparison.Ordinal);
        Assert.False(NetKeyPair.FromFileText(real.ToFileText()).IsDevelopment);

        // Whatever is derived from a development key stays marked, so a derived key cannot launder it.
        Assert.True(dev.DeriveFor(real.PublicKey).IsDevelopment);
        Assert.True(dev.DeriveFor("eu-1").IsDevelopment);
    }

    [Fact]
    public void AKeyFileWhosePublicLineDoesNotMatchOrWhoseKeyIsNotCanonicalTextIsRefused()
    {
        var a = NetKeyPair.Generate();
        var b = NetKeyPair.Generate();
        var text = a.ToFileText().Replace(a.PublicKey.ToString(), b.PublicKey.ToString(), StringComparison.Ordinal);
        Assert.Throws<KeyFormatException>(() => NetKeyPair.FromFileText(text));
        Assert.Throws<KeyFormatException>(() => NetKeyPair.FromFileText("# nothing here\n"));
    }

    [Fact]
    public void ADerivedKeyDependsOnTheServerNameOrKeyAndNothingElse()
    {
        var root = NetKeyPair.FromPrivateKey(new byte[32].Select((_, i) => (byte)(i + 1)).ToArray());
        var server = NetKeyPair.Generate().PublicKey;
        Assert.Equal(root.DeriveFor(server).PublicKey, root.DeriveFor(server).PublicKey);
        Assert.Equal(root.DeriveFor("eu-1").PublicKey, root.DeriveFor("eu-1").PublicKey);
        Assert.NotEqual(root.DeriveFor("eu-1").PublicKey, root.DeriveFor("eu-2").PublicKey);
        Assert.NotEqual(root.DeriveFor("eu-1").PublicKey, root.PublicKey);

        // A name never collides with a key: a server called by the text of a key's bytes is a different derivation.
        var asText = System.Text.Encoding.UTF8.GetString(server.Bytes);
        Assert.NotEqual(root.DeriveFor(server).PublicKey, root.DeriveFor(asText).PublicKey);
        Assert.Throws<ArgumentException>(() => root.DeriveFor(""));
    }

    [Fact]
    public void SavingAKeyWritesOwnerOnlyFilesWhereThePlatformHasModes()
    {
        if (OperatingSystem.IsWindows())
        {
            return;
        }

        var folder = Path.Combine(Path.GetTempPath(), "onegame-keymode-" + Guid.NewGuid().ToString("N"));
        try
        {
            var path = Path.Combine(folder, "a.key");
            NetKeyPair.Generate().Save(path);
            Assert.Equal(UnixFileMode.UserRead | UnixFileMode.UserWrite, File.GetUnixFileMode(path));
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, true);
            }
        }
    }
}
