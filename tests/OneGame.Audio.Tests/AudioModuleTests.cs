using OneGame.Audio;

namespace OneGame.Audio.Tests;

public class AudioModuleTests
{
    [Fact]
    public void ModuleIsNamedAfterItsAssembly() => Assert.Equal("OneGame.Audio", AudioModule.Name);
}
