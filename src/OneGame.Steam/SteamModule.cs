using System.Runtime.CompilerServices;

// Every native call in this assembly passes blittable values, so the runtime never generates marshalling stubs.
[assembly: DisableRuntimeMarshalling]

namespace OneGame.Steam;

/// <summary>Identifies the Steam assembly; a placeholder until its first feature lands.</summary>
public static class SteamModule
{
    /// <summary>The assembly name, <c>OneGame.Steam</c>.</summary>
    public static string Name => "OneGame.Steam";
}
