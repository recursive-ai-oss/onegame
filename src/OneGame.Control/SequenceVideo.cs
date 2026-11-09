using System.Threading.Tasks;
using System.Diagnostics;
using System.Globalization;

namespace OneGame.Control;

internal static class SequenceVideo
{
    internal static async Task<(string? Path, string? SkipReason)> MuxAsync(string directory, int fps, int count)
    {
        if (OperatingSystem.IsBrowser() || OperatingSystem.IsIOS() || OperatingSystem.IsAndroid())
        {
            return (null, "External ffmpeg is not supported on this platform.");
        }

        var executable = FindFfmpeg();
        if (executable is null)
        {
            return (null, "ffmpeg was not found on PATH; PNGs and WAV are available.");
        }

        var path = System.IO.Path.Combine(directory, "video.mp4");
        var start = new ProcessStartInfo(executable)
        {
            UseShellExecute = false,
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            CreateNoWindow = true,
            WorkingDirectory = directory,
        };
        // Even-sized padding permits odd-sized captures without changing their PNGs.
        foreach (var argument in new[] { "-nostdin", "-hide_banner", "-loglevel", "error", "-y",
            "-framerate", fps.ToString(CultureInfo.InvariantCulture), "-i", "frame-%04d.png", "-i", "audio.wav",
            "-map", "0:v:0", "-map", "1:a:0", "-vf", "pad=ceil(iw/2)*2:ceil(ih/2)*2", "-c:v", "libx264",
            "-frames:v", count.ToString(CultureInfo.InvariantCulture), "-threads", "2", "-pix_fmt", "yuv420p", "-c:a", "aac", "-shortest", "video.mp4" })
        {
            start.ArgumentList.Add(argument);
        }

        try
        {
            using var process = Process.Start(start);
            if (process is null)
            {
                return (null, "ffmpeg could not be started; PNGs and WAV are available.");
            }

            var errors = process.StandardError.ReadToEndAsync();
            var output = process.StandardOutput.ReadToEndAsync();
            using var timeout = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            try
            {
                await process.WaitForExitAsync(timeout.Token);
            }
            catch (OperationCanceledException)
            {
                process.Kill(entireProcessTree: true);
                await process.WaitForExitAsync();
                await Task.WhenAll(errors, output);
                File.Delete(path);
                return (null, "ffmpeg exceeded the two-minute mux limit; PNGs and WAV are available.");
            }

            await output;
            var error = await errors;
            if (process.ExitCode == 0 && File.Exists(path))
            {
                return (path, null);
            }

            File.Delete(path);
            return (null, $"ffmpeg exited with code {process.ExitCode}: {error[..Math.Min(error.Length, 1024)].Trim()}");
        }
        catch (Exception e) when (e is System.ComponentModel.Win32Exception or IOException or UnauthorizedAccessException)
        {
            return (null, "ffmpeg could not mux the clip: " + e.Message);
        }
    }

    private static string? FindFfmpeg()
    {
        var name = OperatingSystem.IsWindows() ? "ffmpeg.exe" : "ffmpeg";
        foreach (var directory in (Environment.GetEnvironmentVariable("PATH") ?? "").Split(System.IO.Path.PathSeparator))
        {
            if (!string.IsNullOrWhiteSpace(directory) && File.Exists(System.IO.Path.Combine(directory, name)))
            {
                return System.IO.Path.GetFullPath(System.IO.Path.Combine(directory, name));
            }
        }

        return null;
    }
}
