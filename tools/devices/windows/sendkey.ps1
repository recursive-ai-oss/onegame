# Delivers one key press (down, then up) to a program's window through its own message queue, the way the keyboard driver's messages
# arrive (WM_KEYDOWN / WM_KEYUP with the scan code a keyboard would give), and takes no focus from whatever the person is using:
# sending the key with the real input path would type it into the foreground program instead.
#   sendkey.ps1 -Process Solitaire -VirtualKey 0x72      (F3)
# The platform layer reads keys by scan code, so the scan code is part of the message's lParam, as on a real keyboard.
param([Parameter(Mandatory)] [string] $Process, [int] $VirtualKey = 0x72)
Add-Type @'
using System; using System.Runtime.InteropServices;
public static class Keys {
  [DllImport("user32.dll")] public static extern bool PostMessage(IntPtr h, uint msg, IntPtr wParam, IntPtr lParam);
  [DllImport("user32.dll")] public static extern uint MapVirtualKey(uint code, uint type);
}
'@
$p = Get-Process $Process | Select-Object -First 1
if ($p.MainWindowHandle -eq [IntPtr]::Zero) { 'the program has no window'; exit 3 }
$scan = [Keys]::MapVirtualKey($VirtualKey, 0)
$down = [IntPtr](1 -bor ($scan -shl 16))
$up = [IntPtr](1 -bor ($scan -shl 16) -bor 0xC0000000)
[Keys]::PostMessage($p.MainWindowHandle, 0x100, [IntPtr]$VirtualKey, $down) | Out-Null
Start-Sleep -Milliseconds 80
[Keys]::PostMessage($p.MainWindowHandle, 0x101, [IntPtr]$VirtualKey, $up) | Out-Null
"key $VirtualKey (scan $scan) posted to window $($p.MainWindowHandle)"
