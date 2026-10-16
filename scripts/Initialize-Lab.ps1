#requires -Version 7.2
[CmdletBinding()]
param([switch]$InstallBrowser)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
Push-Location $root
try {
    Write-Host 'Restoring and building .NET sample projects...'
    Invoke-LabNative dotnet @('restore', 'OrdersDemoLab.sln')
    Invoke-LabNative dotnet @('build', 'OrdersDemoLab.sln', '--no-restore', '--nologo')
    foreach ($folder in @('src\Orders.Web', 'tools\contract-mcp')) {
        Push-Location (Join-Path $root $folder)
        try { Invoke-LabNative npm @('ci') } finally { Pop-Location }
    }
    Invoke-LabNative npm @('--prefix', 'src\Orders.Web', 'run', 'build')
    if ($InstallBrowser) {
        Push-Location (Join-Path $root 'src\Orders.Web')
        try { Invoke-LabNative npx @('playwright', 'install', 'chromium') } finally { Pop-Location }
    }
    Write-Host 'Local dependencies ready. No server, cloud resource or scheduled job was started.'
} finally { Pop-Location }
