#requires -Version 7.2
[CmdletBinding()]
param([ValidateRange(1024,65535)][int]$ApiPort = 5100, [ValidateRange(1024,65535)][int]$Port = 5173)
. (Join-Path $PSScriptRoot 'Common.ps1')
$oldTarget = $env:API_PROXY_TARGET
Push-Location (Join-Path (Get-LabRoot) 'src\Orders.Web')
try {
    $env:API_PROXY_TARGET = "http://127.0.0.1:$ApiPort"
    Invoke-LabNative npm @('run','dev','--','--host','127.0.0.1','--port',"$Port",'--strictPort')
} finally {
    $env:API_PROXY_TARGET = $oldTarget
    Pop-Location
}
