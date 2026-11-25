using RecursiveAI.OneGpu;

namespace OneGame.Render2D;

/// <summary>
/// An image the 2D renderer draws into and then samples like any texture: trails, a minimap, a reflection, or a layer drawn once and reused.
/// Make one with <see cref="Renderer2D.CreateRenderTexture"/>. Draw into it between <see cref="Renderer2D.BeginTarget"/> and
/// <see cref="Renderer2D.EndTarget"/>, or point a <see cref="Layer2D"/> at it with <see cref="Layer2D.Target"/>; show it with <see cref="Texture"/>.
/// Its pixels are in the renderer's target format, premultiplied like everything the renderer draws, and persist between frames until cleared.
/// </summary>
public sealed class RenderTexture2D : IDisposable
{
    private readonly GpuTexture _image;
    private bool _disposed;

    internal RenderTexture2D(Renderer2D renderer, GpuTexture image, Texture2D view, Renderer2D.TargetState state)
    {
        Owner = renderer;
        _image = image;
        Texture = view;
        State = state;
        Width = (int)image.Width;
        Height = (int)image.Height;
    }

    /// <summary>Width in pixels.</summary>
    public int Width { get; }

    /// <summary>Height in pixels.</summary>
    public int Height { get; }

    /// <summary>
    /// The image as a texture, for <see cref="Sprite.Texture"/>, tiles and the UI. A render texture must not be drawn while it is the target
    /// being drawn into (the renderer throws <see cref="InvalidOperationException"/>): use two and alternate for feedback.
    /// </summary>
    public Texture2D Texture { get; }

    /// <summary>True once the image has been written to in some frame.</summary>
    internal bool Written { get; set; }

    /// <summary>The last frame a layer cleared this image in, so two layers aimed at one image do not wipe each other.</summary>
    internal int ClearedFrame { get; set; } = -1;

    internal Renderer2D Owner { get; }

    internal Renderer2D.TargetState State { get; }

    internal bool IsDisposed => _disposed;

    /// <summary>Releases the GPU memory. Waits for the device, so it is for resizes and shutdown, not for every frame.</summary>
    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Owner.Untrack(this);
        Owner.Device.WaitIdle();
        Texture.Dispose();
        State.Dispose();
        _image.Dispose();
    }
}
