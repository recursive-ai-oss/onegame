namespace OneGame.Control;

public static partial class ControlOps
{
    private static ControlOp[] OverlayOps =>
    [
        new("overlay",
            "Show, hide or configure the on-screen debug panel (frame time with min/avg/max and a sparkline, tick and entity count, network rtt/bytes per second/drift when connected, audio device counters), as JSON: {visible, key, chord, masked, lines, timing}. While the panel is visible, frames are timed (and only then): timing holds the CPU frame, simulation and draw times of the newest frame with average and maximum over the last 60 frames, a cpuHistory list, and the GPU time per render pass (gpu is 'ready', or 'unsupported' where the backend has no timestamps: Metal and WebGPU). The same timing object is in info while the panel is visible. The panel is drawn over the finished frame, captures included, and is view-only: the state hash is the same with it shown or hidden. visible=true|false sets it, toggle=true flips it. masked=true shows -- for every figure that depends on the machine or the moment (the default in a headless run), so a capture is reproducible. key (a key name such as F4, or 'none') and chord (gamepad buttons joined by +, such as LeftShoulder+RightShoulder, or 'none') change what toggles it for a person at the keyboard or pad; defaults are F3 and Back+Start. lines is the panel's text as built now. Development builds only.",
            """{"type":"object","properties":{"visible":{"type":"boolean"},"toggle":{"type":"boolean"},"masked":{"type":"boolean"},"key":{"type":"string"},"chord":{"type":"string"}}}""", false),
    ];
}
