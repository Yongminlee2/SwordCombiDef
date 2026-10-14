[CmdletBinding()]
param(
    [string]$EditorVersion = "6000.3.18f1"
)

$ErrorActionPreference = "Stop"
$repositoryRoot = [System.IO.Path]::GetFullPath(
    (Join-Path $PSScriptRoot "..")).TrimEnd("\")
$unityInstall = Join-Path $env:LOCALAPPDATA "Unity\Editors\$EditorVersion"
$projectAlias = "C:\SSForge"
$unityAlias = "C:\Unity6000"
$tempAlias = "C:\UnityTemp"

if (!(Test-Path -LiteralPath $projectAlias)) {
    New-Item -ItemType Junction `
        -Path $projectAlias `
        -Target $repositoryRoot | Out-Null
}
if (!(Test-Path -LiteralPath $unityAlias)) {
    New-Item -ItemType Junction `
        -Path $unityAlias `
        -Target $unityInstall | Out-Null
}
if (!(Test-Path -LiteralPath $tempAlias)) {
    New-Item -ItemType Directory -Path $tempAlias | Out-Null
}

$env:TEMP = $tempAlias
$env:TMP = $tempAlias
$resultsPath = Join-Path $projectAlias "Logs\playmode-results.xml"
$logPath = Join-Path $projectAlias "Logs\unity-playmode.log"
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
    "PlayMode",
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
    throw "Unity PlayMode tests failed with exit code $($process.ExitCode). See $logPath."
}
if (!(Test-Path -LiteralPath $resultsPath)) {
    throw "Unity exited without a PlayMode test report. See $logPath."
}
[xml]$results = Get-Content -LiteralPath $resultsPath -Encoding utf8
$run = $results.'test-run'
if ([int]$run.failed -ne 0) {
    throw "Unity PlayMode tests reported $($run.failed) failure(s). See $resultsPath."
}
Write-Output (
    "Unity PlayMode tests passed: {0}/{1}" -f
    $run.passed,
    $run.total)
