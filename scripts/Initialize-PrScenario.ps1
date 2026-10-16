#requires -Version 7.2
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$git = Get-LabGit
$destination = Join-Path $root ".runtime\pr-rehearsal-$([guid]::NewGuid().ToString('N').Substring(0,8))"
[void](New-Item -ItemType Directory -Path $destination)
Copy-LabSource $root $destination
function Git-Step {
    param([string[]]$GitArguments)
    Invoke-LabNative $git (@('-C',$destination,'-c',"core.hooksPath=$destination\.git\disabled-hooks",
        '-c','core.autocrlf=false') + $GitArguments)
}
function Demo-Commit {
    param([string]$Message)
    Git-Step @('-c','user.name=Demo Author','-c','user.email=demo@example.invalid',
        '-c','commit.gpgsign=false','commit','--quiet','-m',$Message,
        '-m','Co-authored-by: Copilot <223556219+Copilot@users.noreply.github.com>')
}
Git-Step @('init','--quiet','--initial-branch=main')
[void](New-Item -ItemType Directory -Path (Join-Path $destination '.git\disabled-hooks') -Force)
Git-Step @('config','--local','core.hooksPath',(Join-Path $destination '.git\disabled-hooks'))
Git-Step @('config','--local','core.autocrlf','false')
Git-Step @('add','.')
Demo-Commit 'Synthetic lab baseline'
$stateFile = Join-Path $destination 'demos\06-pr-quality\pr-checkpoint.json'
Git-Step @('switch','--quiet','-c','feature/idempotency')
'{"mode":"payload","retentionHours":24,"description":"Feature: preserve retry intent"}' | Set-Content -LiteralPath $stateFile -Encoding utf8
Git-Step @('add','demos/06-pr-quality/pr-checkpoint.json')
Demo-Commit 'Feature checkpoint with intentionally failing payload acceptance'
Git-Step @('switch','--quiet','main')
'{"mode":"fixed","retentionHours":48,"description":"Target: extend retention policy"}' | Set-Content -LiteralPath $stateFile -Encoding utf8
Git-Step @('add','demos/06-pr-quality/pr-checkpoint.json')
Demo-Commit 'Target branch changes the same policy line'
Git-Step @('switch','--quiet','feature/idempotency')
Write-Host "Local-only rehearsal created: $destination"
Write-Host 'No remote exists; no branch was pushed. main and feature/idempotency now diverge.'
Write-Host 'Run scripts\Invoke-PrChecks.ps1 to see the intentional failing check.'
Write-Host 'Run git merge main INSIDE this disposable checkout to produce the deliberate conflict.'
Write-Output $destination
