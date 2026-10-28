using OneGame.RootMotionProbe;

using var stream = typeof(Probe).Assembly.GetManifestResourceStream("figure.glb")!;
using var copy = new MemoryStream();
stream.CopyTo(copy);
Console.Write(Probe.Run(copy.ToArray()));
