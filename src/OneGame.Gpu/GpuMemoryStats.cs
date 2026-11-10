namespace OneGame.Gpu;

/// <summary>A backend's tally of device memory, from <see cref="GpuDevice.MemoryStats"/>.</summary>
/// <param name="ReservedBytes">Bytes held from the driver (blocks that resources are carved from).</param>
/// <param name="UsedBytes">Bytes occupied by live resources, with alignment padding.</param>
/// <param name="Allocations">Number of live resource allocations.</param>
/// <param name="StagingBytes">Bytes of host-visible upload staging memory the backend holds right now (a subset of <paramref name="ReservedBytes"/>); bounded by <see cref="GpuDevice.StagingBudgetBytes"/> apart from single uploads larger than the budget.</param>
public readonly record struct GpuMemoryStats(ulong ReservedBytes, ulong UsedBytes, int Allocations, ulong StagingBytes = 0);
