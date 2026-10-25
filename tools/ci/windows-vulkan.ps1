# Gives a Windows machine without a GPU a Vulkan device, for the GPU and windowed tests.
#
# Two downloads, each pinned by version and checked by SHA-256:
#   - the Vulkan loader (vulkan-1.dll) from LunarG's runtime components, because a machine without a GPU driver has none;
#   - Mesa's lavapipe, a CPU Vulkan driver, from a Windows build of Mesa.
# Both are test tools; nothing here ships. The script writes VK_DRIVER_FILES and PATH to $GITHUB_ENV (when set)
# so the steps after it see the driver.
param(
    [string] $Destination = (Join-Path $env:RUNNER_TEMP 'vulkan')
)
$ErrorActionPreference = 'Stop'

$loaderUrl = 'https://sdk.lunarg.com/sdk/download/1.4.363.0/windows/vulkan-runtime-components.zip'
$loaderSha = 'a25a927aa8b9f0371048f1861cf88ac3b9bc9b1fb332c42d897c8ab32695769a'
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
$loaderDir = Join-Path $Destination 'loader/VulkanRT-X64-1.4.363.0-Components/x64'

$mesaDir = Join-Path $Destination 'mesa'
# The runner image ships 7-Zip.
& 7z x $mesa7z "-o$mesaDir" 'x64/vulkan_lvp.dll' 'x64/lvp_icd.x86_64.json' -y | Out-Null
if ($LASTEXITCODE -ne 0) { throw '7z failed to extract lavapipe' }
$icd = Join-Path $mesaDir 'x64/lvp_icd.x86_64.json'

Write-Host "Vulkan loader: $loaderDir"
Write-Host "lavapipe driver manifest: $icd"
if ($env:GITHUB_ENV) {
    "VK_DRIVER_FILES=$icd" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
    "VK_ICD_FILENAMES=$icd" | Out-File -Append -Encoding utf8 $env:GITHUB_ENV
}
if ($env:GITHUB_PATH) {
    $loaderDir | Out-File -Append -Encoding utf8 $env:GITHUB_PATH
}
