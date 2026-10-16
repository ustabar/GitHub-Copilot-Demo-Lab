#requires -Version 7.2
[CmdletBinding()]
param([ValidateRange(1,720)][int]$Hours = 24, [switch]$IncludeBrowser)
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$run = Get-Date -Format 'yyyyMMdd-HHmmss-fff'
$output = Join-Path $root "artifacts\quality-gate\$run"
[void](New-Item -ItemType Directory -Path $output -Force)
$checks = [System.Collections.Generic.List[object]]::new()
$report = [System.Collections.Generic.List[string]]::new()
$report.Add("# Quality gate — $run")
$report.Add('')
$report.Add("Scope: current checkout; HEAD commits since $Hours hours ago, plus current working-tree changes.")
$report.Add('Synthetic lab only. This script never edits source code, commits, pushes or merges.')
$report.Add('Tests and builds may write local output under bin/obj, dist, .runtime and artifacts.')
$report.Add('')
$oldMode = $env:ORDERS_TEST_MODE
function Run-Check {
    param([string]$Name, [string]$Executable, [string[]]$Arguments)
    $log = Join-Path $output "$Name.log"
    & $Executable @Arguments 2>&1 | Tee-Object -FilePath $log | Out-Host
    $code = $LASTEXITCODE
    $checks.Add([pscustomobject]@{ name=$Name; exitCode=$code; log=$log })
    $report.Add("- ${Name}: $(if ($code -eq 0) { 'PASS' } else { "FAIL (exit $code)" }) — $Name.log")
}
Push-Location $root
try {
    $env:ORDERS_TEST_MODE = 'fixed'
    $git = Get-LabGit
    $inside = & $git rev-parse --is-inside-work-tree 2>$null
    if ($LASTEXITCODE -eq 0 -and $inside -eq 'true') {
        $changes = @(& $git --no-pager log "--since=$Hours hours ago" --format='COMMIT %h %s' --name-only HEAD)
        if ($LASTEXITCODE -ne 0) { throw 'Git history could not be read. Create a local initial commit or use the source-only directory.' }
        $changes += @(& $git status --short)
        if ($LASTEXITCODE -ne 0) { throw 'Git working-tree status could not be read.' }
        $changes | Set-Content -LiteralPath (Join-Path $output 'changes.txt') -Encoding utf8
        $report.Add('Change evidence: changes.txt. Tests below cover the full small lab, not a guessed selective subset.')
    } else {
        $report.Add('Change evidence: UNAVAILABLE (this directory is not a Git checkout). No 24-hour coverage claim is made; full lab checks run.')
        Write-Warning 'No Git history: reporting full-check fallback explicitly, not inventing a 24-hour diff.'
    }
    $report.Add('')
    Run-Check 'api-tests' 'dotnet' @('test','tests\Orders.Api.Tests\Orders.Api.Tests.csproj','--nologo')
    Run-Check 'web-build' 'npm' @('--prefix','src\Orders.Web','run','build')
    Run-Check 'mcp-tests' 'npm' @('--prefix','tools\contract-mcp','test')
    if ($IncludeBrowser) { Run-Check 'browser-tests' 'npm' @('--prefix','src\Orders.Web','run','test:e2e') }
    else { $report.Add('- browser-tests: NOT RUN (use -IncludeBrowser).') }
    $report.Add('')
    $report.Add('AI review: NOT RUN. This deterministic script does not call Copilot or invent review findings.')
    $report.Add('Review gaps: compare changed behavior with fixtures acceptance criteria; record untested cases explicitly.')
} finally {
    $env:ORDERS_TEST_MODE = $oldMode
    Pop-Location
    $report | Set-Content -LiteralPath (Join-Path $output 'report.md') -Encoding utf8
    $checks | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $output 'checks.json') -Encoding utf8
    Write-Host "Report: $(Join-Path $output 'report.md')"
}
if (@($checks | Where-Object exitCode -ne 0).Count -gt 0) { exit 1 }
