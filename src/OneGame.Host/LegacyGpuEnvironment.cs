namespace OneGame.Host;

/// <summary>
/// Keeps the GPU environment variable names that onegame 1.x documented working. The GPU layer reads the
/// <c>ONEGPU_*</c> names; a process that only sets an old <c>ONEGAME_*</c> name gets it copied across before the
/// first device is created. A new name that is already set always wins.
/// </summary>
internal static class LegacyGpuEnvironment
{
    private static readonly (string Old, string New)[] Names =
    [
        ("ONEGAME_GPU_VALIDATION", "ONEGPU_VALIDATION"),
        ("ONEGAME_GPU_ADAPTER", "ONEGPU_ADAPTER"),
        ("ONEGAME_GPU_BACKEND", "ONEGPU_BACKEND"),
        ("ONEGAME_VALIDATION_OPTIONAL", "ONEGPU_VALIDATION_OPTIONAL"),
        ("ONEGAME_VULKAN_LIBRARY", "ONEGPU_VULKAN_LIBRARY"),
    ];

    /// <summary>Copies each old variable that is set to its new name when the new name is not set.</summary>
    internal static void Apply()
    {
        foreach (var (old, current) in Names)
        {
            if (Environment.GetEnvironmentVariable(current) is null && Environment.GetEnvironmentVariable(old) is { } value)
            {
                Environment.SetEnvironmentVariable(current, value);
            }
        }
    }
}
