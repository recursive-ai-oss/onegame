using System.Runtime.InteropServices;

namespace OneGame.Platform;

/// <summary>
/// Gives a program built for the Windows GUI subsystem (no console window of its own, the default for released Windows games) somewhere to
/// print when it was started from a terminal. Everywhere else, and when the parent already redirected the output to a pipe or a file,
/// it does nothing.
/// </summary>
public static partial class ParentConsole
{
    private const int StdInput = -10, StdOutput = -11, StdError = -12;
    private const uint AttachParentProcess = 0xFFFFFFFF;
    private const uint GenericRead = 0x80000000, GenericWrite = 0x40000000, ShareRead = 1, ShareWrite = 2, OpenExisting = 3;

    /// <summary>
    /// Attaches to the console of the process that started this one, so <see cref="Console.Out"/> and <see cref="Console.Error"/> reach the
    /// terminal. Call it before the first use of <see cref="Console"/>; <c>GameApp.Run</c> does. Output the parent redirected is left alone,
    /// and a program started from Explorer or a game launcher has no parent console, so the call fails quietly and nothing is printed.
    /// </summary>
    /// <returns>True when a console was attached.</returns>
    public static bool Attach()
    {
        if (!OperatingSystem.IsWindows())
        {
            return false;
        }

        // A GUI process gets a null handle for an output the parent did not redirect (an invalid one for a closed handle).
        var output = GetStdHandle(StdOutput);
        var error = GetStdHandle(StdError);
        var needOutput = output is 0 or -1;
        var needError = error is 0 or -1;
        if (!needOutput && !needError)
        {
            return false;
        }

        if (AttachConsole(AttachParentProcess) == 0)
        {
            return false;
        }

        if (needOutput)
        {
            Redirect(StdOutput, Open("CONOUT$"));
        }

        if (needError)
        {
            Redirect(StdError, Open("CONOUT$"));
        }

        if (GetStdHandle(StdInput) is 0 or -1)
        {
            Redirect(StdInput, Open("CONIN$"));
        }

        return true;
    }

    private static nint Open(string name) => CreateFile(name, GenericRead | GenericWrite, ShareRead | ShareWrite, 0, OpenExisting, 0, 0);

    private static void Redirect(int which, nint handle)
    {
        if (handle != -1 && handle != 0)
        {
            _ = SetStdHandle(which, handle);
        }
    }

    [LibraryImport("kernel32.dll", EntryPoint = "GetStdHandle")]
    private static partial nint GetStdHandle(int which);

    [LibraryImport("kernel32.dll", EntryPoint = "SetStdHandle")]
    private static partial int SetStdHandle(int which, nint handle);

    [LibraryImport("kernel32.dll", EntryPoint = "AttachConsole")]
    private static partial int AttachConsole(uint processId);

    [LibraryImport("kernel32.dll", EntryPoint = "CreateFileW", StringMarshalling = StringMarshalling.Utf16)]
    private static partial nint CreateFile(string name, uint access, uint share, nint security, uint disposition, uint flags, nint template);
}
