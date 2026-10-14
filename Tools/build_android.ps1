[CmdletBinding()]
param(
    [ValidateSet("DevelopmentApk", "ReleaseAab")]
    [string]$Mode = "DevelopmentApk",

    [string]$EditorVersion = "6000.3.18f1"
)

$ErrorActionPreference = "Stop"

function Ensure-Junction {
    param(
        [Parameter(Mandatory = $true)]
        [string]$Path,

        [Parameter(Mandatory = $true)]
        [string]$Target
    )

    $targetPath = [System.IO.Path]::GetFullPath($Target).TrimEnd("\")
    if (Test-Path -LiteralPath $Path) {
        $item = Get-Item -LiteralPath $Path -Force
        $existingTarget = [string]($item.Target | Select-Object -First 1)
        if ($item.LinkType -ne "Junction" -or
            ![string]::Equals(
                $existingTarget.TrimEnd("\"),
                $targetPath,
                [System.StringComparison]::OrdinalIgnoreCase)) {
            throw "$Path exists and is not a junction to $targetPath."
        }

        return
    }

    New-Item -ItemType Junction -Path $Path -Target $targetPath | Out-Null
}

$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")).TrimEnd("\")
$unityInstall = Join-Path $env:LOCALAPPDATA "Unity\Editors\$EditorVersion"

if (!(Test-Path -LiteralPath (Join-Path $unityInstall "Editor\Unity.exe"))) {
    throw "Unity $EditorVersion was not found at $unityInstall."
}

$projectAlias = "C:\SSBuildClean061"
$unityAlias = "C:\Unity6000"
$gradleAlias = "C:\SSGradleHome061"
$javaHomeAlias = "C:\GradleJavaHome"
$tempAlias = "C:\UnityTemp"

if (Test-Path -LiteralPath $projectAlias) {
    $stagingItem = Get-Item -LiteralPath $projectAlias -Force
    if (($stagingItem.Attributes -band
            [System.IO.FileAttributes]::ReparsePoint) -ne 0) {
        throw "$projectAlias must be a real directory, not a junction."
    }
} else {
    New-Item -ItemType Directory -Path $projectAlias | Out-Null
}

foreach ($directoryName in @("Assets", "Packages", "ProjectSettings")) {
    $sourceDirectory = Join-Path $repositoryRoot $directoryName
    $stagingDirectory = Join-Path $projectAlias $directoryName
    & robocopy `
        $sourceDirectory `
        $stagingDirectory `
        /MIR `
        /NFL `
        /NDL `
        /NJH `
        /NJS `
        /NP | Out-Host
    if ($LASTEXITCODE -gt 7) {
        throw "Failed to stage $directoryName with robocopy exit code $LASTEXITCODE."
    }
}

Ensure-Junction -Path $unityAlias -Target $unityInstall

if (!(Test-Path -LiteralPath $tempAlias)) {
    New-Item -ItemType Directory -Path $tempAlias | Out-Null
}
if (!(Test-Path -LiteralPath $javaHomeAlias)) {
    New-Item -ItemType Directory -Path $javaHomeAlias | Out-Null
}
if (!(Test-Path -LiteralPath $gradleAlias)) {
    New-Item -ItemType Directory -Path $gradleAlias | Out-Null
}

$env:GRADLE_USER_HOME = $gradleAlias
$env:JAVA_HOME = Join-Path $unityAlias (
    "Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK")
$env:GRADLE_OPTS =
    "-Dgradle.user.home=$gradleAlias -Duser.home=$javaHomeAlias"
$env:TEMP = $tempAlias
$env:TMP = $tempAlias
$tempOption = "-Djava.io.tmpdir=$tempAlias"
$userHomeOption = "-Duser.home=$javaHomeAlias"
$gradleHomeOption = "-Dgradle.user.home=$gradleAlias"
$env:JAVA_TOOL_OPTIONS =
    (($env:JAVA_TOOL_OPTIONS, $tempOption, $userHomeOption, $gradleHomeOption) |
        Where-Object { ![string]::IsNullOrWhiteSpace($_) }) -join " "

$unityExe = Join-Path $unityAlias "Editor\Unity.exe"
$javaExe = Join-Path $unityAlias (
    "Editor\Data\PlaybackEngines\AndroidPlayer\OpenJDK\bin\java.exe")
$gradleLauncher = Join-Path $unityAlias (
    "Editor\Data\PlaybackEngines\AndroidPlayer\Tools\gradle\lib\" +
    "gradle-launcher-9.1.0.jar")

& $javaExe -classpath $gradleLauncher org.gradle.launcher.GradleMain --stop |
    Out-Host

$method = if ($Mode -eq "ReleaseAab") {
    "Swordstellation.Editor.ProjectSetup.BuildReleaseBundle"
} else {
    "Swordstellation.Editor.ProjectSetup.BuildDevelopmentApk"
}
$stagedArtifact = if ($Mode -eq "ReleaseAab") {
    Join-Path $projectAlias "Builds\Android\Swordstellation-release.aab"
} else {
    Join-Path $projectAlias "Builds\Android\Swordstellation-development.apk"
}
$artifact = if ($Mode -eq "ReleaseAab") {
    Join-Path $repositoryRoot "Builds\Android\Swordstellation-release.aab"
} else {
    Join-Path $repositoryRoot "Builds\Android\Swordstellation-development.apk"
}

$logsDirectory = Join-Path $projectAlias "Logs"
New-Item -ItemType Directory -Path $logsDirectory -Force | Out-Null
$logPath = Join-Path $logsDirectory "unity-build-$($Mode.ToLowerInvariant()).log"
$outputLogsDirectory = Join-Path $repositoryRoot "Logs"
New-Item -ItemType Directory -Path $outputLogsDirectory -Force | Out-Null
$outputLogPath = Join-Path $outputLogsDirectory (
    "unity-build-$($Mode.ToLowerInvariant()).log")
$arguments = @(
    "-batchmode",
    "-nographics",
    "-quit",
    "-projectPath",
    $projectAlias,
    "-executeMethod",
    $method,
    "-logFile",
    $logPath
)

$process = Start-Process `
    -FilePath $unityExe `
    -ArgumentList $arguments `
    -PassThru `
    -Wait `
    -WindowStyle Hidden

if (Test-Path -LiteralPath $logPath) {
    Copy-Item `
        -LiteralPath $logPath `
        -Destination $outputLogPath `
        -Force
}
if ($process.ExitCode -ne 0) {
    throw "Unity build failed with exit code $($process.ExitCode). See $outputLogPath."
}

if (!(Test-Path -LiteralPath $stagedArtifact)) {
    throw "Unity exited successfully, but the expected artifact is missing: $stagedArtifact"
}

$artifactDirectory = Split-Path -Parent $artifact
New-Item -ItemType Directory -Path $artifactDirectory -Force | Out-Null
Copy-Item `
    -LiteralPath $stagedArtifact `
    -Destination $artifact `
    -Force
$artifactInfo = Get-Item -LiteralPath $artifact
Write-Output (
    "Build succeeded: {0} ({1:N1} MB)" -f
    $artifactInfo.FullName,
    ($artifactInfo.Length / 1MB))
