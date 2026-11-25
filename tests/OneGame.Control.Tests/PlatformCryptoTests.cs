using System.Reflection.Metadata;
using System.Reflection.PortableExecutable;

namespace OneGame.Control.Tests;

/// <summary>
/// Phones have no system crypto library: the runtime's random, hash and HMAC wrappers abort the process there instead of throwing
/// (an Android app that built the live-control server died in the OpenSSL shim at startup). Every assembly a game ships next to
/// the control layer must reach those primitives only through the engine's managed
/// ones (<c>Entropy</c>, <c>Sha256</c>, <c>HmacSha256</c>). The one exception is Entropy's Windows source, where the provider is
/// part of the OS. The control library's dependency (OneModelContext) signs its request state with platform crypto by design;
/// since 1.0.2 it builds that state lazily, which the phone run of live control covers, so it is not scanned here.
/// </summary>
public sealed class PlatformCryptoTests
{
    private const string Banned = "System.Security.Cryptography";

    /// <summary>Lists every reference a phone-reachable assembly makes to a type in System.Security.Cryptography.</summary>
    internal static List<string> Offenders(string directory)
    {
        var found = new List<string>();
        foreach (var path in Directory.EnumerateFiles(directory, "*.dll").Order(StringComparer.Ordinal))
        {
            var file = Path.GetFileName(path);
            // Test assemblies run on the build machine only.
            if (!file.StartsWith("OneGame.", StringComparison.Ordinal) || file.EndsWith(".Tests.dll", StringComparison.Ordinal))
            {
                continue;
            }

            using var stream = File.OpenRead(path);
            using var pe = new PEReader(stream);
            if (!pe.HasMetadata)
            {
                continue;
            }

            var md = pe.GetMetadataReader();
            foreach (var handle in md.TypeReferences)
            {
                var r = md.GetTypeReference(handle);
                if (md.GetString(r.Namespace) != Banned)
                {
                    continue;
                }

                var name = md.GetString(r.Name);
                var allowed = file == "OneGame.Core.dll" && name == "RandomNumberGenerator";
                if (!allowed)
                {
                    found.Add($"{file} uses {Banned}.{name}");
                }
            }
        }

        return found;
    }

    [Fact]
    public void NoPhoneReachableAssemblyCallsThePlatformCryptoProvider()
    {
        var offenders = Offenders(AppContext.BaseDirectory);
        Assert.True(offenders.Count == 0, "Use OneGame.Core.Entropy / Sha256 / HmacSha256 instead:\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void TheScanFindsAPlatformRandomCall()
    {
        // Prove the scanner on an assembly that really uses the provider.
        var dir = Path.Combine(Path.GetTempPath(), "crypto-scan-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            // The test assembly itself references SHA256 in Sha256HexTests, so as "OneGame.*.dll" it must be reported.
            var self = typeof(PlatformCryptoTests).Assembly.Location;
            File.Copy(self, Path.Combine(dir, "OneGame.Probe.dll"));
            Assert.Contains(Offenders(dir), o => o.StartsWith("OneGame.Probe.dll uses", StringComparison.Ordinal));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }
}
