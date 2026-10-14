[CmdletBinding()]
param(
    [ValidateSet("DevelopmentApk", "ReleaseAab")]
    [string]$Mode = "DevelopmentApk"
)

$buildScript = Join-Path $PSScriptRoot "build_android.ps1"
$arguments = @(
    "-NoProfile",
    "-ExecutionPolicy",
    "Bypass",
    "-File",
    "`"$buildScript`"",
    "-Mode",
    $Mode
)
$process = Start-Process `
    -FilePath "powershell.exe" `
    -ArgumentList $arguments `
    -PassThru `
    -WindowStyle Hidden
Write-Output "Started Android build process $($process.Id) in $Mode mode."
