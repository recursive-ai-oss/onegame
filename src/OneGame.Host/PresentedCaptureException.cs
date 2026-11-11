namespace OneGame.Host;

/// <summary>Why a presented frame could not be captured.</summary>
public enum PresentedCaptureFailure
{
    /// <summary>No failure.</summary>
    None,
    /// <summary>The game has no native window to present to.</summary>
    NoWindow,
    /// <summary>The window is minimized, so nothing is drawn.</summary>
    Minimized,
    /// <summary>The window system has stopped drawing the window (covered or hidden); drawing waits so the sim and network keep running.</summary>
    WindowNotDrawn,
    /// <summary>The swapchain gave no free image within its acquire timeout.</summary>
    NoBackbuffer,
    /// <summary>The window system took the surface; a new swapchain is made on the next frame.</summary>
    SurfaceLost,
}

/// <summary>A presented capture was asked for from a game that has a window and a graphics device, but no frame could be drawn and read back.</summary>
public sealed class PresentedCaptureException : InvalidOperationException
{
    /// <summary>Creates the exception for a reason.</summary>
    /// <param name="reason">Why no frame was presented.</param>
    public PresentedCaptureException(PresentedCaptureFailure reason)
        : base(Describe(reason)) => Reason = reason;

    /// <summary>Why no frame was presented.</summary>
    public PresentedCaptureFailure Reason { get; }

    static string Describe(PresentedCaptureFailure r) => r switch
    {
        PresentedCaptureFailure.NoWindow => "No frame was presented: the game has no native window. Capture without 'presented' to render offscreen.",
        PresentedCaptureFailure.Minimized => "No frame was presented: the window is minimized. Restore it, or capture without 'presented'.",
        PresentedCaptureFailure.WindowNotDrawn => "No frame was presented: the window system has stopped drawing the window (covered or hidden). Show it, or capture without 'presented'.",
        PresentedCaptureFailure.NoBackbuffer => "No frame was presented: the swapchain gave no free image in time. The window system may not be releasing images; capture without 'presented' to render offscreen.",
        PresentedCaptureFailure.SurfaceLost => "No frame was presented: the window system took the surface. Try again after the next frame.",
        _ => "No frame was presented.",
    };
}
