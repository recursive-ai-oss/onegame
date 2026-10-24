namespace OneGame.Platform;
internal sealed class WaylandBackend : PlatformBackend { public WaylandBackend(PlatformApp a):base(a){} public override PlatformBackendKind Kind => PlatformBackendKind.Wayland; public override Window CreateWindow(WindowOptions o,int id)=>throw new NotImplementedException(); public override void Pump(int t){} }
internal sealed class Win32Backend : PlatformBackend { public Win32Backend(PlatformApp a):base(a){} public override PlatformBackendKind Kind => PlatformBackendKind.Win32; public override Window CreateWindow(WindowOptions o,int id)=>throw new NotImplementedException(); public override void Pump(int t){} }
#pragma warning disable CA1822
internal sealed class XInputGamepads { public XInputGamepads(PlatformApp a, Func<int> n){} public void Poll(){} public bool Rumble(int i,float l,float h)=>false; }
