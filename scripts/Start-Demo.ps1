#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('01','02','03','04','05','06','07','08','09')][string]$Id,
    [ValidateSet('fixed','baseline')][string]$Checkpoint = 'fixed',
    [ValidateRange(1024,65535)][int]$Port = 5100
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$demo = Get-DemoDefinition $Id
$mode = if ($Checkpoint -eq 'baseline') { $demo.baselineMode } else { 'fixed' }
$project = Join-Path $root "demos\$($demo.folder)\Orders.Demo$Id.csproj"
$directory = Join-Path $root ".runtime\demo-$Id-$Checkpoint"
[void](New-Item -ItemType Directory -Path $directory -Force)
$database = Join-Path $directory 'orders.db'
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$oldDotnetEnvironment = $env:DOTNET_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    Write-Host "Demo $Id | checkpoint=$Checkpoint | mode=$mode | http://127.0.0.1:$Port"
    Write-Host "Database: $database (preserved between restarts). Ctrl+C stops only this host."
    if ($mode -ne 'fixed') { Write-Warning 'INTENTIONALLY INCORRECT CHECKPOINT. Synthetic local demo only.' }
    Invoke-LabNative dotnet @('run', '--project', $project, '--no-launch-profile', '--',
        '--urls', "http://127.0.0.1:$Port", '--Orders:Mode', $mode, '--Orders:DatabasePath', $database)
} finally {
    $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment
    $env:DOTNET_ENVIRONMENT = $oldDotnetEnvironment
}
