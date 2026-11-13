using OneGame.Host;

namespace OneGame.Testing.Tests;

/// <summary>The 1.x GPU variable names keep working: they are copied to the names the GPU layer reads.</summary>
public sealed class LegacyGpuEnvironmentTests
{
    private const string Old = "ONEGAME_VALIDATION_OPTIONAL";
    private const string New = "ONEGPU_VALIDATION_OPTIONAL";

    [Fact]
    public void AnOldNameIsCopiedToTheNewOneAndANewNameWins()
    {
        var oldBefore = Environment.GetEnvironmentVariable(Old);
        var newBefore = Environment.GetEnvironmentVariable(New);
        try
        {
            Environment.SetEnvironmentVariable(New, null);
            Environment.SetEnvironmentVariable(Old, "1");
            LegacyGpuEnvironment.Apply();
            Assert.Equal("1", Environment.GetEnvironmentVariable(New));

            Environment.SetEnvironmentVariable(New, "0");
            LegacyGpuEnvironment.Apply();
            Assert.Equal("0", Environment.GetEnvironmentVariable(New));
        }
        finally
        {
            Environment.SetEnvironmentVariable(Old, oldBefore);
            Environment.SetEnvironmentVariable(New, newBefore);
        }
    }
}
