namespace OneGame.Gpu.WebGpu;

// Argument checks that raise the same typed exceptions as the native backends, before anything is recorded.
internal static class Validate
{
    public static void TextureDesc(in TextureDesc desc, in GpuCapabilities caps)
    {
        if (desc.Width == 0 || desc.Height == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "texture size must be greater than zero");
        }

        if (desc.MipLevels == 0 || desc.ArrayLayers == 0 || desc.SampleCount == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(desc), "mip levels, array layers and sample count must be at least 1 (use new TextureDesc(...) rather than default)");
        }

        if (desc.SampleCount > 1 && (desc.MipLevels > 1 || desc.ArrayLayers > 1 || desc.Dimension != TextureDimension.D2))
        {
            throw new ArgumentException("multisampled textures are 2D with one mip level and one layer", nameof(desc));
        }

        if (desc.SampleCount > caps.MaxSampleCount)
        {
            throw new GpuUnsupportedException($"WebGPU supports at most {caps.MaxSampleCount} samples per texel, not {desc.SampleCount}");
        }

        var cube = desc.Dimension == TextureDimension.Cube;
        var volume = desc.Dimension == TextureDimension.D3;
        if (cube && (desc.Width != desc.Height || desc.ArrayLayers % 6 != 0))
        {
            throw new ArgumentException("a cube texture is square with a multiple of 6 layers", nameof(desc));
        }

        if (volume && (desc.ArrayLayers != 1 || desc.Depth == 0 || (desc.Usage & TextureUsage.RenderTarget) != 0))
        {
            throw new ArgumentException("a 3D texture has one layer, a depth of at least 1, and cannot be a render target", nameof(desc));
        }

        if (!volume && desc.Depth != 1)
        {
            throw new ArgumentException("only 3D textures have a depth", nameof(desc));
        }

        if (Math.Max(desc.Width, desc.Height) > (volume ? caps.MaxTextureSize3D : caps.MaxTextureSize) || (volume && desc.Depth > caps.MaxTextureSize3D) || desc.ArrayLayers > caps.MaxArrayLayers)
        {
            throw new GpuUnsupportedException($"texture size {desc.Width}x{desc.Height}x{(volume ? desc.Depth : desc.ArrayLayers)} exceeds what this browser's adapter supports");
        }

        if (desc.MipLevels > (volume ? GpuTexture.FullMipCount(desc.Width, desc.Height, desc.Depth) : GpuTexture.FullMipCount(desc.Width, desc.Height)))
        {
            throw new ArgumentException("more mip levels than the texture size has", nameof(desc));
        }
    }

    public static void Subresource(GpuTexture t, uint mip, uint layer)
    {
        if (mip >= t.MipLevels || layer >= t.ArrayLayers)
        {
            throw new ArgumentOutOfRangeException(nameof(mip), "mip level or layer out of range");
        }
    }

    public static void Write(GpuTexture t, in TextureRegion region, int length)
    {
        if ((t.Usage & TextureUsage.CopyDestination) == 0)
        {
            throw new ArgumentException("texture was not created with TextureUsage.CopyDestination", nameof(t));
        }

        Subresource(t, region.MipLevel, region.Layer);
        var lw = Math.Max(t.Width >> (int)region.MipLevel, 1);
        var lh = Math.Max(t.Height >> (int)region.MipLevel, 1);
        var ld = Math.Max(t.Depth >> (int)region.MipLevel, 1);
        if (region.Width == 0 || region.Height == 0 || region.Depth == 0
            || (ulong)region.X + region.Width > lw || (ulong)region.Y + region.Height > lh || (ulong)region.Z + region.Depth > ld)
        {
            throw new ArgumentOutOfRangeException(nameof(region), $"region lies outside mip {region.MipLevel} ({lw}x{lh}x{ld})");
        }

        if (t.Dimension != TextureDimension.D3 && (region.Z != 0 || region.Depth != 1))
        {
            throw new ArgumentOutOfRangeException(nameof(region), "only 3D textures have slices; use Layer for arrays and cube faces");
        }

        var expected = (ulong)region.Width * region.Height * region.Depth * GpuDevice.BytesPerTexel(t.Format);
        if ((ulong)length != expected)
        {
            throw new ArgumentException($"expected {expected} bytes of tightly packed texel rows for a {region.Width}x{region.Height}x{region.Depth} region, got {length}", nameof(length));
        }
    }

    /// <summary>Checks a bind group against its layout and returns how many dynamic offsets it takes.</summary>
    public static int BindGroup(WebPipelineLayout layout, uint setIndex, ReadOnlySpan<BindGroupEntry> entries)
    {
        if (setIndex >= layout.SetCount)
        {
            throw new ArgumentOutOfRangeException(nameof(setIndex), $"layout has {layout.SetCount} sets");
        }

        var declared = layout.Bindings.Where(b => b.Set == setIndex).ToArray();
        if (declared.Length != entries.Length)
        {
            throw new ArgumentException($"setIndex {setIndex} declares {declared.Length} bindings but {entries.Length} entries were given", nameof(entries));
        }

        var dynamic = 0;
        foreach (var d in declared)
        {
            var found = false;
            foreach (var e in entries)
            {
                if (e.Binding != d.Binding)
                {
                    continue;
                }

                found = true;
                switch (d.Kind)
                {
                    case ResourceKind.ConstantBuffer or ResourceKind.StorageBuffer or ResourceKind.ReadOnlyStorageBuffer:
                        var buffer = e.Buffer ?? throw new ArgumentException($"binding {d.Binding} needs a buffer", nameof(entries));
                        var need = d.Kind == ResourceKind.ConstantBuffer ? BufferUsage.Uniform : BufferUsage.Storage;
                        if ((buffer.Usage & need) == 0)
                        {
                            throw new ArgumentException($"buffer at binding {d.Binding} lacks BufferUsage.{need}", nameof(entries));
                        }

                        if (d.Dynamic)
                        {
                            dynamic++;
                            if (e.Size == 0)
                            {
                                throw new ArgumentException($"dynamic binding {d.Binding} needs an explicit Size: the range that moves with each offset", nameof(entries));
                            }
                        }

                        break;
                    case ResourceKind.SampledTexture or ResourceKind.StorageTexture:
                        var texture = e.Texture ?? throw new ArgumentException($"binding {d.Binding} needs a texture", nameof(entries));
                        var needT = d.Kind == ResourceKind.SampledTexture ? TextureUsage.Sampled : TextureUsage.Storage;
                        if ((texture.Usage & needT) == 0)
                        {
                            throw new ArgumentException($"texture at binding {d.Binding} lacks TextureUsage.{needT}", nameof(entries));
                        }

                        View(texture, e.View ?? default);
                        break;
                    case ResourceKind.Sampler:
                        _ = e.Sampler ?? throw new ArgumentException($"binding {d.Binding} needs a sampler", nameof(entries));
                        break;
                }
            }

            if (!found)
            {
                throw new ArgumentException($"no entry for binding {d.Binding} of setIndex {setIndex}", nameof(entries));
            }
        }

        return dynamic;
    }

    public static void View(GpuTexture t, in TextureViewDesc d)
    {
        var levels = d.MipCount == 0 ? t.MipLevels - Math.Min(d.BaseMip, t.MipLevels) : d.MipCount;
        var layers = t.Dimension == TextureDimension.D3 ? 1u : d.LayerCount == 0 ? t.ArrayLayers - Math.Min(d.BaseLayer, t.ArrayLayers) : d.LayerCount;
        if (d.BaseMip + levels > t.MipLevels || d.BaseLayer + layers > t.ArrayLayers || levels == 0 || layers == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(d), "texture view range extends past the texture's mip levels or layers");
        }

        var kind = d.Kind;
        if (kind == TextureViewKind.Auto)
        {
            return;
        }

        if (kind == TextureViewKind.D3 != (t.Dimension == TextureDimension.D3))
        {
            throw new ArgumentException("only 3D textures have 3D views, and 3D textures only have 3D views", nameof(d));
        }

        if ((kind == TextureViewKind.Cube && layers != 6) || (kind == TextureViewKind.CubeArray && (layers % 6 != 0 || layers == 0)) || (kind == TextureViewKind.D2 && layers != 1))
        {
            throw new ArgumentException($"a {kind} view cannot show {layers} layers", nameof(d));
        }

        if (kind is TextureViewKind.Cube or TextureViewKind.CubeArray && t.Dimension != TextureDimension.Cube)
        {
            throw new ArgumentException("cube views need a cube texture", nameof(d));
        }
    }
}
