# Gives a Windows machine without a GPU a working Vulkan setup for the GPU and windowed tests.
#
# Downloads, each pinned by version and checked by SHA-256:
#   - LunarG's loader-only Vulkan runtime (vulkan-1.dll), because a machine without a GPU driver has none;
#   - Mesa's lavapipe, a CPU Vulkan driver, from a Windows build of Mesa.
# Optionally the SDK installer is tried for the Khronos validation layer; if it yields one, tests run with validation,
# otherwise the tests run without it (ONEGAME_VALIDATION_OPTIONAL) and say so. Linux CI always validates.
# All of these are test tools; nothing here ships. The script writes the environment the later steps need to $GITHUB_ENV.
param(
    [string] $Destination = (Join-Path $env:RUNNER_TEMP 'vulkan')
)
$ErrorActionPreference = 'Stop'

$loaderUrl = 'https://sdk.lunarg.com/sdk/download/1.4.363.0/windows/vulkan-runtime-components.zip'
$loaderSha = 'a25a927aa8b9f0371048f1861cf88ac3b9bc9b1fb332c42d897c8ab32695769a'
$sdkUrl = 'https://sdk.lunarg.com/sdk/download/1.4.363.0/windows/vulkan_sdk.exe'
$sdkSha = '94a82d378f7a5e3e54c9db7d2fb7016af136e14ac0a18dbf0f2f67a36352d141'
$mesaUrl = 'https://github.com/pal1000/mesa-dist-win/releases/download/26.2.4/mesa3d-26.2.4-release-msvc.7z'
$mesaSha = '351fc8c8b695878ffb3eaa044b3ead08672a48b1a045e3c3e3975811df0f6695'

function Get-Pinned($url, $sha, $file) {
    Invoke-WebRequest -Uri $url -OutFile $file -UseBasicParsing
    $actual = (Get-FileHash -Algorithm SHA256 $file).Hash.ToLowerInvariant()
    if ($actual -ne $sha) { throw "$url has SHA-256 $actual, expected $sha" }
}

New-Item -ItemType Directory -Force $Destination | Out-Null
$loaderZip = Join-Path $Destination 'loader.zip'
$mesa7z = Join-Path $Destination 'mesa.7z'
Get-Pinned $loaderUrl $loaderSha $loaderZip
Get-Pinned $mesaUrl $mesaSha $mesa7z

Expand-Archive -Path $loaderZip -DestinationPath (Join-Path $Destination 'loader') -Force
$bin = Join-Path $Destination 'loader/VulkanRT-X64-1.4.363.0-Components/x64'
if (-not (Test-Path (Join-Path $bin 'vulkan-1.dll'))) { throw "no vulkan-1.dll in $bin" }

$mesaDir = Join-Path $Destination 'mesa'
# The runner image ships 7-Zip.
& 7z x $mesa7z "-o$mesaDir" 'x64/vulkan_lvp.dll' 'x64/lvp_icd.x86_64.json' -y | Out-Null
if ($LASTEXITCODE -ne 0) { throw '7z failed to extract lavapipe' }
$icd = Join-Path $mesaDir 'x64/lvp_icd.x86_64.json'

# Best effort: the SDK installer's command-line "copy only" mode, for the validation layer.
$layerDir = $null
try {
    Get-Pinned $sdkUrl $sdkSha (Join-Path $Destination 'vulkan_sdk.exe')
    $sdkRoot = Join-Path $Destination 'sdk'
    $p = Start-Process -FilePath (Join-Path $Destination 'vulkan_sdk.exe') -Wait -PassThru -ArgumentList @(
        '--root', $sdkRoot, '--accept-licenses', '--default-answer', '--confirm-command', 'install', 'copy_only=1', 'com.lunarg.vulkan.core')
    Write-Host "SDK installer exit code $($p.ExitCode)"
    $layer = Get-ChildItem -Recurse -Path $sdkRoot -Filter 'VkLayer_khronos_validation.json' -ErrorAction SilentlyContinue | Select-Object -First 1
    if ($layer) { $layerDir = $layer.DirectoryName }
} catch {
    Write-Host "SDK installer step failed: $_"
}
if ($layerDir) { Write-Host "validation layer: $layerDir" } else { Write-Host 'validation layer: none; tests run without it' }

# The loader ignores VK_DRIVER_FILES and VK_ADD_LAYER_PATH when the process is elevated, and the runner is an administrator.
# Windows drivers announce themselves in the registry, so announce the software driver and the layer the same way:
# a REG_DWORD value 0 named with the manifest's full path (the Vulkan loader's documented Windows discovery).
function Register-Manifest($key, $path) {
    $full = (Resolve-Path $path).Path
    New-Item -Path $key -Force | Out-Null
    New-ItemProperty -Path $key -Name $full -PropertyType DWord -Value 0 -Force | Out-Null
    Write-Host "registered $full under $key"
}
Register-Manifest 'HKLM:\SOFTWARE\Khronos\Vulkan\Drivers' $icd
if ($layerDir) {
    Register-Manifest 'HKLM:\SOFTWARE\Khronos\Vulkan\ExplicitLayers' (Join-Path $layerDir 'VkLayer_khronos_validation.json')
}

$env:VK_DRIVER_FILES = $icd
$env:VK_LOADER_DEBUG = 'error,warn,driver'
Write-Host "Vulkan loader: $bin\vulkan-1.dll"
Write-Host "lavapipe driver manifest: $icd"
& (Join-Path $bin 'vulkaninfo.exe') --summary 2>&1 | Select-Object -First 40
Write-Host "vulkaninfo exit code $LASTEXITCODE"

if ($env:GITHUB_ENV) {
    "VK_DRIVER_FILES=$icd" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    "ONEGAME_VULKAN_LIBRARY=$bin\vulkan-1.dll" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    if ($layerDir) {
        "VK_ADD_LAYER_PATH=$layerDir" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    } else {
        "ONEGAME_VALIDATION_OPTIONAL=1" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    }
}
if ($env:GITHUB_PATH) {
    $bin | Out-File -Append -Encoding utf8 $env:GITHUB_PATH
}
