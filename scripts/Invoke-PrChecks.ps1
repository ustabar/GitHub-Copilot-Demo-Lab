#requires -Version 7.2
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$config = Get-Content -LiteralPath (Join-Path (Get-LabRoot) 'demos\06-pr-quality\pr-checkpoint.json') -Raw | ConvertFrom-Json
if ($config.mode -notin @('fixed','payload')) { throw 'Invalid PR rehearsal mode.' }
$oldMode = $env:ORDERS_TEST_MODE
try {
    $env:ORDERS_TEST_MODE = $config.mode
    & dotnet test (Join-Path (Get-LabRoot) 'tests\Orders.Api.Tests\Orders.Api.Tests.csproj') --filter 'TestCategory=Payload' --nologo
    $code = $LASTEXITCODE
} finally { $env:ORDERS_TEST_MODE = $oldMode }
exit $code
