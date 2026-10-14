param([string]$UnityRoot = "")

$ErrorActionPreference = "Stop"
if ([string]::IsNullOrWhiteSpace($UnityRoot)) {
    $UnityRoot = Join-Path $env:LOCALAPPDATA "Unity\Editors\6000.3.18f1"
}

$androidRoot = Join-Path $UnityRoot "Editor\Data\PlaybackEngines\AndroidPlayer"
$sdkRoot = Join-Path $androidRoot "SDK"
$downloadRoot = Join-Path $env:TEMP "SwordstellationAndroidModules"

New-Item -ItemType Directory -Path $downloadRoot -Force | Out-Null
New-Item -ItemType Directory -Path $sdkRoot -Force | Out-Null

function Assert-UnderRoot {
    param([string]$Path, [string]$Root)

    $fullPath = [System.IO.Path]::GetFullPath($Path)
    $fullRoot = [System.IO.Path]::GetFullPath($Root)
    if (-not $fullPath.StartsWith($fullRoot, [System.StringComparison]::OrdinalIgnoreCase)) {
        throw "Refusing to operate outside the expected root: $fullPath"
    }
}

function Remove-TaskPath {
    param([string]$Path)

    Assert-UnderRoot $Path $downloadRoot
    if (Test-Path -LiteralPath $Path) {
        Remove-Item -LiteralPath $Path -Recurse -Force
    }
}

function Remove-TaskFile {
    param([string]$Path)

    Assert-UnderRoot $Path $downloadRoot
    if (-not (Test-Path -LiteralPath $Path)) {
        return
    }

    for ($attempt = 1; $attempt -le 10; $attempt++) {
        try {
            Remove-Item -LiteralPath $Path -Force
            return
        }
        catch {
            if ($attempt -eq 10) {
                throw
            }
            Start-Sleep -Milliseconds 500
        }
    }
}

function Download-Archive {
    param([string]$Name, [string]$Url)

    $archive = Join-Path $downloadRoot "$Name.zip"
    Assert-UnderRoot $archive $downloadRoot
    if ((Test-Path -LiteralPath $archive) -and
        (Get-Item -LiteralPath $archive).Length -ge 1024) {
        Write-Host "Reusing downloaded archive: $Name"
        return $archive
    }
    Remove-TaskFile $archive

    Write-Host "Downloading $Name"
    & curl.exe --fail --location --retry 5 --retry-delay 2 --connect-timeout 30 `
        --output $archive $Url
    if ($LASTEXITCODE -ne 0 -or -not (Test-Path -LiteralPath $archive) -or
        (Get-Item -LiteralPath $archive).Length -lt 1024) {
        throw "Download failed for $Name."
    }
    return $archive
}

function Expand-ToStaging {
    param([string]$Name, [string]$Archive)

    $staging = Join-Path $downloadRoot "$Name-extract"
    Remove-TaskPath $staging
    New-Item -ItemType Directory -Path $staging -Force | Out-Null

    Write-Host "Extracting $Name"
    & tar.exe -xf $Archive -C $staging
    if ($LASTEXITCODE -ne 0) {
        throw "Extraction failed for $Name."
    }
    return $staging
}

function Install-SingleRootArchive {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Destination,
        [string]$ValidationPath
    )

    if (Test-Path -LiteralPath $ValidationPath) {
        Write-Host "Already installed: $Name"
        return
    }

    $archive = Download-Archive $Name $Url
    $staging = Expand-ToStaging $Name $archive
    $roots = @(Get-ChildItem -LiteralPath $staging -Force)
    if ($roots.Count -ne 1 -or -not $roots[0].PSIsContainer) {
        throw "$Name did not contain exactly one root directory."
    }

    Assert-UnderRoot $Destination $androidRoot
    if (Test-Path -LiteralPath $Destination) {
        throw "Destination already exists but is incomplete: $Destination"
    }
    New-Item -ItemType Directory -Path (Split-Path -Parent $Destination) -Force | Out-Null
    [System.IO.Directory]::Move($roots[0].FullName, $Destination)

    Remove-TaskPath $staging
    Remove-TaskFile $archive
    if (-not (Test-Path -LiteralPath $ValidationPath)) {
        throw "$Name validation failed: $ValidationPath"
    }
}

function Install-MergeArchive {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Destination,
        [string]$ValidationPath
    )

    if (Test-Path -LiteralPath $ValidationPath) {
        Write-Host "Already installed: $Name"
        return
    }

    $archive = Download-Archive $Name $Url
    $staging = Expand-ToStaging $Name $archive
    Assert-UnderRoot $Destination $androidRoot
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & robocopy.exe $staging $Destination /E /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -gt 7) {
        throw "Copy failed for $Name with robocopy exit code $LASTEXITCODE."
    }

    Remove-TaskPath $staging
    Remove-TaskFile $archive
    if (-not (Test-Path -LiteralPath $ValidationPath)) {
        throw "$Name validation failed: $ValidationPath"
    }
}

function Install-FlatArchive {
    param(
        [string]$Name,
        [string]$Url,
        [string]$Destination,
        [string]$ValidationPath
    )

    if (Test-Path -LiteralPath $ValidationPath) {
        Write-Host "Already installed: $Name"
        return
    }

    $archive = Download-Archive $Name $Url
    $staging = Expand-ToStaging $Name $archive
    Assert-UnderRoot $Destination $androidRoot
    if (Test-Path -LiteralPath $Destination) {
        throw "Destination already exists but is incomplete: $Destination"
    }
    New-Item -ItemType Directory -Path $Destination -Force | Out-Null
    & robocopy.exe $staging $Destination /E /NFL /NDL /NJH /NJS /NP
    if ($LASTEXITCODE -gt 7) {
        throw "Copy failed for $Name with robocopy exit code $LASTEXITCODE."
    }

    Remove-TaskPath $staging
    Remove-TaskFile $archive
    if (-not (Test-Path -LiteralPath $ValidationPath)) {
        throw "$Name validation failed: $ValidationPath"
    }
}

Install-SingleRootArchive `
    "openjdk-17" `
    "https://download.unity3d.com/download_unity/open-jdk/open-jdk-win-x64/jdk17.0.18-8_15e8817d1f5db6db3571ebe7430ef37f7fa8e60e8ff6f3e18ca1cb4c29f78774.zip" `
    (Join-Path $androidRoot "OpenJDK") `
    (Join-Path $androidRoot "OpenJDK\bin\java.exe")

Install-MergeArchive `
    "sdk-tools" `
    "https://dl.google.com/android/repository/sdk-tools-windows-4333796.zip" `
    $sdkRoot `
    (Join-Path $sdkRoot "tools\bin\sdkmanager.bat")

Install-SingleRootArchive `
    "ndk-r27c" `
    "https://dl.google.com/android/repository/android-ndk-r27c-windows.zip" `
    (Join-Path $androidRoot "NDK") `
    (Join-Path $androidRoot "NDK\source.properties")

Install-FlatArchive `
    "cmake-3.22.1" `
    "https://dl.google.com/android/repository/cmake-3.22.1-windows.zip" `
    (Join-Path $sdkRoot "cmake\3.22.1") `
    (Join-Path $sdkRoot "cmake\3.22.1\bin\cmake.exe")

Install-SingleRootArchive `
    "build-tools-36" `
    "https://dl.google.com/android/repository/build-tools_r36_windows.zip" `
    (Join-Path $sdkRoot "build-tools\36.0.0") `
    (Join-Path $sdkRoot "build-tools\36.0.0\aapt2.exe")

Install-SingleRootArchive `
    "platform-tools-36" `
    "https://dl.google.com/android/repository/platform-tools_r36.0.0-win.zip" `
    (Join-Path $sdkRoot "platform-tools") `
    (Join-Path $sdkRoot "platform-tools\adb.exe")

Install-SingleRootArchive `
    "platform-34" `
    "https://dl.google.com/android/repository/platform-34-ext7_r02.zip" `
    (Join-Path $sdkRoot "platforms\android-34") `
    (Join-Path $sdkRoot "platforms\android-34\android.jar")

Install-SingleRootArchive `
    "platform-35" `
    "https://dl.google.com/android/repository/platform-35_r01.zip" `
    (Join-Path $sdkRoot "platforms\android-35") `
    (Join-Path $sdkRoot "platforms\android-35\android.jar")

Install-SingleRootArchive `
    "platform-36" `
    "https://dl.google.com/android/repository/platform-36_r02.zip" `
    (Join-Path $sdkRoot "platforms\android-36") `
    (Join-Path $sdkRoot "platforms\android-36\android.jar")

Install-SingleRootArchive `
    "command-line-tools-16" `
    "https://dl.google.com/android/repository/commandlinetools-win-12266719_latest.zip" `
    (Join-Path $sdkRoot "cmdline-tools\16.0") `
    (Join-Path $sdkRoot "cmdline-tools\16.0\bin\sdkmanager.bat")

Write-Output "Android module installation complete."
