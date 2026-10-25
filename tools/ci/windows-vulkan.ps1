# Gives a Windows machine without a GPU a working Vulkan setup for the GPU and windowed tests.
#
# Two downloads, each pinned by version and checked by SHA-256:
#   - the LunarG Vulkan SDK (loader, Khronos validation layer, vulkaninfo), installed silently from its command line;
#   - Mesa's lavapipe, a CPU Vulkan driver, from a Windows build of Mesa.
# Both are test tools; nothing here ships. The script writes the environment the later steps need to $GITHUB_ENV.
param(
    [string] $Destination = (Join-Path $env:RUNNER_TEMP 'vulkan')
)
$ErrorActionPreference = 'Stop'

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
$sdkExe = Join-Path $Destination 'vulkan_sdk.exe'
$mesa7z = Join-Path $Destination 'mesa.7z'
Get-Pinned $sdkUrl $sdkSha $sdkExe
Get-Pinned $mesaUrl $mesaSha $mesa7z

$sdkRoot = Join-Path $Destination 'sdk'
$install = Start-Process -FilePath $sdkExe -Wait -PassThru -ArgumentList @(
    '--root', $sdkRoot, '--accept-licenses', '--default-answer', '--confirm-command', 'install')
Write-Host "SDK installer exit code $($install.ExitCode)"

# The installer may hand the work to another process or ignore --root, so look for the loader in the places it could be,
# for up to five minutes.
$searchRoots = @($sdkRoot, 'C:\VulkanSDK', (Join-Path $env:ProgramFiles 'VulkanSDK'), (Join-Path $env:RUNNER_TEMP 'VulkanSDK'))
$loader = $null
$deadline = (Get-Date).AddMinutes(5)
while (-not $loader -and (Get-Date) -lt $deadline) {
    foreach ($root in $searchRoots) {
        if (Test-Path $root) {
            $loader = Get-ChildItem -Recurse -Path $root -Filter 'vulkan-1.dll' -ErrorAction SilentlyContinue |
                Where-Object { $_.FullName -match '\\Bin\\' } | Select-Object -First 1
            if ($loader) { break }
        }
    }
    if (-not $loader) { Start-Sleep -Seconds 5 }
}
if (-not $loader) {
    Write-Host 'vulkan-1.dll was not found. Folders that exist:'
    Get-ChildItem C:\ -Directory | ForEach-Object { Write-Host "  $($_.FullName)" }
    foreach ($root in $searchRoots) { if (Test-Path $root) { Write-Host "contents of ${root}:"; Get-ChildItem -Recurse $root -ErrorAction SilentlyContinue | Select-Object -First 40 | ForEach-Object { Write-Host "  $($_.FullName)" } } }
    throw 'the SDK install produced no Bin\vulkan-1.dll'
}
$bin = $loader.DirectoryName
Write-Host "SDK Bin folder: $bin"

$mesaDir = Join-Path $Destination 'mesa'
# The runner image ships 7-Zip.
& 7z x $mesa7z "-o$mesaDir" 'x64/vulkan_lvp.dll' 'x64/lvp_icd.x86_64.json' -y | Out-Null
if ($LASTEXITCODE -ne 0) { throw '7z failed to extract lavapipe' }
$icd = Join-Path $mesaDir 'x64/lvp_icd.x86_64.json'

$env:VK_DRIVER_FILES = $icd
$env:VK_ADD_LAYER_PATH = $bin
$env:VK_LOADER_DEBUG = 'error,warn,driver'
Write-Host "Vulkan loader: $bin\vulkan-1.dll"
Write-Host "lavapipe driver manifest: $icd"
Write-Host "validation layer: $(Test-Path (Join-Path $bin 'VkLayer_khronos_validation.dll'))"
# What the loader sees, so a failing run says why.
& (Join-Path $bin 'vulkaninfo.exe') --summary 2>&1 | Select-Object -First 40
Write-Host "vulkaninfo exit code $LASTEXITCODE"

if ($env:GITHUB_ENV) {
    "VK_DRIVER_FILES=$icd" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    "VK_ADD_LAYER_PATH=$bin" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    "ONEGAME_VULKAN_LIBRARY=$bin\vulkan-1.dll" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
}
if ($env:GITHUB_PATH) {
    $bin | Out-File -Append -Encoding utf8 $env:GITHUB_PATH
}
