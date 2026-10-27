# Prepares a Windows machine for the device check, entirely under one user folder (nothing system-wide):
#   - a .NET SDK of the version global.json asks for, from the official install script;
#   - the Khronos validation layer, from LunarG's SDK installer in its "copy only" mode (pinned, SHA-256 checked), unless -NoValidation;
#   - env.cmd, which the check's jobs call first: .NET on PATH, a build output folder, and the C++ linker for NativeAOT when the machine
#     has Visual Studio build tools that the .NET toolchain does not find by itself (it is told to use the environment's tools).
# Everything is skipped when it is already there. Run it with: powershell -NoProfile -ExecutionPolicy Bypass -File setup.ps1 -Base <folder> -SdkVersion <x.y.z>
param(
    [Parameter(Mandatory)] [string] $Base,
    [Parameter(Mandatory)] [string] $SdkVersion,
    [switch] $NoValidation
)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
New-Item -ItemType Directory -Force $Base | Out-Null
$dotnet = Join-Path $Base 'dotnet'

if (-not (Test-Path (Join-Path $dotnet "sdk\$SdkVersion"))) {
    $script = Join-Path $Base 'dotnet-install.ps1'
    Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $script
    & powershell -NoProfile -ExecutionPolicy Bypass -File $script -Version $SdkVersion -InstallDir $dotnet | Out-Null
}
"dotnet $SdkVersion at $dotnet"

$layerDir = Join-Path $Base 'vk\sdk\Bin'
if (-not $NoValidation -and -not (Test-Path (Join-Path $layerDir 'VkLayer_khronos_validation.json'))) {
    $sdk = Join-Path $Base 'vk\vulkan_sdk.exe'
    New-Item -ItemType Directory -Force (Split-Path $sdk) | Out-Null
    Invoke-WebRequest -UseBasicParsing -Uri 'https://sdk.lunarg.com/sdk/download/1.4.363.0/windows/vulkan_sdk.exe' -OutFile $sdk
    $sha = (Get-FileHash -Algorithm SHA256 $sdk).Hash.ToLowerInvariant()
    if ($sha -ne '94a82d378f7a5e3e54c9db7d2fb7016af136e14ac0a18dbf0f2f67a36352d141') { throw "vulkan_sdk.exe has SHA-256 $sha" }
    Start-Process -FilePath $sdk -Wait -ArgumentList @('--root', (Join-Path $Base 'vk\sdk'), '--accept-licenses', '--default-answer', '--confirm-command', 'install', 'copy_only=1', 'com.lunarg.vulkan.core') | Out-Null
    Remove-Item $sdk -Force
}
if (Test-Path (Join-Path $layerDir 'VkLayer_khronos_validation.json')) { "validation layer at $layerDir" } else { 'validation layer: none' }

# The C++ environment: the first Visual Studio install that has the x64 tools.
$vcvars = $null
foreach ($root in @("${env:ProgramFiles(x86)}\Microsoft Visual Studio", "$env:ProgramFiles\Microsoft Visual Studio")) {
    if (-not (Test-Path $root)) { continue }
    foreach ($bat in Get-ChildItem -Path $root -Recurse -Filter vcvars64.bat -ErrorAction SilentlyContinue) {
        $tools = Join-Path (Split-Path (Split-Path (Split-Path $bat.FullName))) 'Tools\MSVC'
        if (Test-Path $tools) { $vcvars = $bat.FullName; break }
    }
    if ($vcvars) { break }
}

$env1 = @(
    '@echo off',
    "set DOTNET_ROOT=$dotnet",
    "set PATH=$dotnet;%PATH%",
    'set DOTNET_CLI_TELEMETRY_OPTOUT=1',
    'set DOTNET_NOLOGO=1',
    'set MSBUILDDISABLENODEREUSE=1',
    "set ONEGAME_ARTIFACTS=$Base\art",
    "set NUGET_PACKAGES=$Base\nuget"
)
if ($vcvars) {
    # vcvars sets Platform=x64, which makes the solution's configuration lookup fail; the .NET toolchain is told to take the linker from
    # the environment because it does not find build tools without the C++ workload registered.
    $env1 += "call `"$vcvars`" >nul"
    $env1 += 'set Platform='
    $env1 += 'set IlcUseEnvironmentalTools=true'
    $env1 += "set PATH=$dotnet;%PATH%"
    "C++ tools: $vcvars"
} else {
    'C++ tools: none found (NativeAOT exports will not link)'
}
Set-Content -Path (Join-Path $Base 'env.cmd') -Value $env1 -Encoding ascii
