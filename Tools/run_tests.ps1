[CmdletBinding()]
param(
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
$projectAlias = "C:\SSForge"
$unityAlias = "C:\Unity6000"
$tempAlias = "C:\UnityTemp"

Ensure-Junction -Path $projectAlias -Target $repositoryRoot
Ensure-Junction -Path $unityAlias -Target $unityInstall
if (!(Test-Path -LiteralPath $tempAlias)) {
    New-Item -ItemType Directory -Path $tempAlias | Out-Null
}

$env:TEMP = $tempAlias
$env:TMP = $tempAlias
$resultsPath = Join-Path $projectAlias "Logs\editmode-results.xml"
$logPath = Join-Path $projectAlias "Logs\unity-editmode.log"
if (Test-Path -LiteralPath $resultsPath) {
    Remove-Item -LiteralPath $resultsPath -Force
}
$arguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath",
    $projectAlias,
    "-runTests",
    "-testPlatform",
    "EditMode",
    "-testResults",
    $resultsPath,
    "-logFile",
    $logPath
)

$process = Start-Process `
    -FilePath (Join-Path $unityAlias "Editor\Unity.exe") `
    -ArgumentList $arguments `
    -PassThru `
    -Wait `
    -WindowStyle Hidden

if ($process.ExitCode -ne 0) {
    throw "Unity tests failed with exit code $($process.ExitCode). See $logPath."
}
if (!(Test-Path -LiteralPath $resultsPath)) {
    throw "Unity exited without a test report. See $logPath."
}

[xml]$results = Get-Content -LiteralPath $resultsPath -Encoding utf8
$run = $results.'test-run'
if ([int]$run.failed -ne 0) {
    throw "Unity tests reported $($run.failed) failure(s). See $resultsPath."
}

Write-Output (
    "Unity EditMode tests passed: {0}/{1}" -f
    $run.passed,
    $run.total)

$playModeResultsPath = Join-Path $projectAlias "Logs\playmode-results.xml"
$playModeLogPath = Join-Path $projectAlias "Logs\unity-playmode.log"
if (Test-Path -LiteralPath $playModeResultsPath) {
    Remove-Item -LiteralPath $playModeResultsPath -Force
}
$playModeArguments = @(
    "-batchmode",
    "-nographics",
    "-projectPath",
    $projectAlias,
    "-runTests",
    "-testPlatform",
    "PlayMode",
    "-testResults",
    $playModeResultsPath,
    "-logFile",
    $playModeLogPath
)
$playModeProcess = Start-Process `
    -FilePath (Join-Path $unityAlias "Editor\Unity.exe") `
    -ArgumentList $playModeArguments `
    -PassThru `
    -Wait `
    -WindowStyle Hidden

if ($playModeProcess.ExitCode -ne 0) {
    throw "Unity PlayMode tests failed with exit code $($playModeProcess.ExitCode). See $playModeLogPath."
}
if (!(Test-Path -LiteralPath $playModeResultsPath)) {
    throw "Unity exited without a PlayMode test report. See $playModeLogPath."
}
[xml]$playModeResults = Get-Content -LiteralPath $playModeResultsPath -Encoding utf8
$playModeRun = $playModeResults.'test-run'
if ([int]$playModeRun.failed -ne 0) {
    throw "Unity PlayMode tests reported $($playModeRun.failed) failure(s). See $playModeResultsPath."
}
Write-Output (
    "Unity PlayMode tests passed: {0}/{1}" -f
    $playModeRun.passed,
    $playModeRun.total)

dotnet run `
    --project (Join-Path $repositoryRoot "Tools\CoreSmoke\CoreSmoke.csproj") `
    -c Release
