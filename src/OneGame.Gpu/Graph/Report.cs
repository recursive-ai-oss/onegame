namespace OneGame.Gpu.Graph;

/// <summary>One thing a pass touches, as shown in a report.</summary>
/// <param name="Resource">Resource name.</param>
/// <param name="Access">How: Sampled, StorageRead, StorageWrite, ColorTarget, DepthTarget, ResolveTarget, CopySource, CopyDestination, Uniform, Vertex, Index, Indirect, Write or ReadWrite.</param>
/// <param name="Mip">Mip level for render targets; 0 otherwise.</param>
/// <param name="Layer">Array layer or cube face for render targets; 0 otherwise.</param>
public sealed record AccessReport(string Resource, string Access, uint Mip, uint Layer);

/// <summary>One pass of the last frame.</summary>
/// <param name="Name">Pass name.</param>
/// <param name="Type"><c>raster</c>, <c>compute</c> or <c>custom</c>.</param>
/// <param name="Culled">True if nothing used its results, so it was not recorded.</param>
/// <param name="Order">Position among the passes that ran, or -1 when culled.</param>
/// <param name="Reads">What it reads (including what it loads and keeps).</param>
/// <param name="Writes">What it writes.</param>
/// <param name="GpuMicroseconds">GPU time in microseconds from the newest finished frame that ran this pass; null when unknown (culled, timing off, or not finished yet).</param>
public sealed record PassReport(string Name, string Type, bool Culled, int Order, AccessReport[] Reads, AccessReport[] Writes, double? GpuMicroseconds);

/// <summary>One texture or buffer of the last frame.</summary>
/// <param name="Name">Resource name.</param>
/// <param name="Kind"><c>texture</c> or <c>buffer</c>.</param>
/// <param name="Imported">True when the graph does not own it.</param>
/// <param name="Used">True when a pass that ran touches it (always true for imports).</param>
/// <param name="Format">Texture format; null for buffers.</param>
/// <param name="Size">Texture size as WxH, WxHxLayers or WxHxDepth; the byte size for buffers.</param>
/// <param name="Bytes">Memory it needs, counting all mips, layers and samples.</param>
/// <param name="Slot">Physical allocation it lives in; resources with the same slot share memory. -1 for imports and unused resources.</param>
/// <param name="FirstPass">First pass that ran and touched it.</param>
/// <param name="LastPass">Last pass that ran and touched it.</param>
/// <param name="Usage">Flags the memory was created with.</param>
public sealed record ResourceReport(string Name, string Kind, bool Imported, bool Used, string? Format, string Size, ulong Bytes, int Slot, string? FirstPass, string? LastPass, string Usage);

/// <summary>What <see cref="RenderGraph.Describe"/> returns: the structure of the last frame.</summary>
/// <param name="Frame">Frame number.</param>
/// <param name="Timing">Whether GPU timing is on.</param>
/// <param name="TimingFrame">Frame the GPU times come from (0 when there are none); it can trail <paramref name="Frame"/> by a frame or two.</param>
/// <param name="Passes">Every pass added, in declaration order, culled ones included.</param>
/// <param name="Resources">Every resource declared.</param>
/// <param name="Stats">Totals.</param>
public sealed record RenderGraphReport(int Frame, bool Timing, int TimingFrame, PassReport[] Passes, ResourceReport[] Resources, GraphStats Stats);
