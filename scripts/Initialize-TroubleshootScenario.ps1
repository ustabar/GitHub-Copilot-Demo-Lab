#requires -Version 7.2
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$destination = Join-Path $root ".runtime\troubleshoot-$([guid]::NewGuid().ToString('N').Substring(0,8))"
Copy-LabSource $root $destination
Copy-Item -LiteralPath (Join-Path $root 'demos\07-troubleshoot\api.instructions.broken.txt') `
    -Destination (Join-Path $destination '.github\instructions\api.instructions.md') -Force
Write-Host "Disposable local lab: $destination"
Write-Host 'Only this copy has the deliberately wrong applyTo scope. No VS Code settings were enabled.'
Write-Output $destination
