#requires -Version 7.2
[CmdletBinding(SupportsShouldProcess, ConfirmImpact = 'High')]
param(
    [Parameter(Mandatory)][ValidateSet('01','02','03','04','05','06','07','08','09')][string]$Id,
    [ValidateSet('fixed','baseline')][string]$Checkpoint = 'fixed'
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$directory = Join-Path (Get-LabRoot) ".runtime\demo-$Id-$Checkpoint"
if (-not (Test-Path -LiteralPath $directory)) {
    Write-Host "No stored data for demo $Id / $Checkpoint."
    return
}
Write-Warning 'Stop the corresponding demo host with Ctrl+C before confirming. Never reset between the two halves of a restart test.'
if ($PSCmdlet.ShouldProcess($directory, 'Delete this synthetic demo checkpoint database directory')) {
    Remove-Item -LiteralPath $directory -Recurse -Force
    Write-Host "Demo $Id / $Checkpoint data cleared."
}
