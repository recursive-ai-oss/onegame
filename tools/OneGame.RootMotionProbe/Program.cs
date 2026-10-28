using OneGame.RootMotionProbe;

Console.Write(Probe.Run(File.ReadAllBytes(Path.Combine(AppContext.BaseDirectory, "figure.glb"))));
