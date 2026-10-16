#requires -Version 7.2
[CmdletBinding()]
param(
    [Parameter(Mandatory)][ValidateSet('03','04','05')][string]$Id,
    [Parameter(Mandatory)][ValidateSet('baseline','fixed')][string]$Checkpoint
)
. (Join-Path $PSScriptRoot 'Common.ps1')
$demo = Get-DemoDefinition $Id
$mode = if ($Checkpoint -eq 'baseline') { $demo.baselineMode } else { 'fixed' }
$category = @{ '03'='Retry'; '04'='Persistence'; '05'='Payload' }[$Id]
$oldMode = $env:ORDERS_TEST_MODE
try {
    $env:ORDERS_TEST_MODE = $mode
    Write-Host "Demo $Id | mode=$mode | fixed acceptance criteria | TestCategory=$category"
    if ($mode -ne 'fixed') { Write-Warning 'A failing acceptance test is EXPECTED. The exit code remains nonzero; no failure is hidden.' }
    & dotnet test (Join-Path (Get-LabRoot) 'tests\Orders.Api.Tests\Orders.Api.Tests.csproj') `
        --filter "TestCategory=$category" --nologo
    $result = $LASTEXITCODE
} finally { $env:ORDERS_TEST_MODE = $oldMode }
exit $result
