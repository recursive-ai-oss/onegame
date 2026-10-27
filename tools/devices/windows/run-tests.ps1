# Runs built test apps (all of them, or the names given) one after the other, each with a timeout, and prints a line per app.
#   run-tests.ps1 -Base <folder holding art\bin> [-Apps OneGame.Gpu.Vulkan.Tests,OneGame.Render2D.Tests]
param([Parameter(Mandatory)] [string] $Base, [string[]] $Apps = @(), [int] $TimeoutSeconds = 1500)
# `powershell -File` passes a comma list as one string.
$Apps = @($Apps | ForEach-Object { $_ -split ',' })
$logs = Join-Path $Base 'art\test-logs'
New-Item -ItemType Directory -Force $logs | Out-Null
$env:ONEGAME_CLUSTER_LOGS = Join-Path $logs 'cluster'
foreach ($d in Get-ChildItem (Join-Path $Base 'art\bin') -Directory -Filter '*Tests') {
    $n = $d.Name
    if ($Apps.Count -gt 0 -and ($Apps -notcontains $n)) { continue }
    $dir = Join-Path $d.FullName 'debug'
    $exe = Join-Path $dir "$n.exe"
    if (-not (Test-Path $exe)) { continue }
    $p = Start-Process -FilePath $exe -ArgumentList '-noColor' -WorkingDirectory $dir -RedirectStandardOutput "$logs\$n.log" -RedirectStandardError "$logs\$n.err" -PassThru -WindowStyle Hidden
    $null = $p.Handle   # without this the exit code reads empty after a timed wait
    if (-not $p.WaitForExit($TimeoutSeconds * 1000)) { $p.Kill($true); "$n TIMEOUT"; continue }
    $sum = (Select-String -Path "$logs\$n.log" -Pattern 'Total:' | Select-Object -Last 1).Line.Trim()
    "$n exit=$($p.ExitCode) $sum"
}
