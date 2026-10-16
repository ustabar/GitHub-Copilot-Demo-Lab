#requires -Version 7.2
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$manifest = Get-Content -LiteralPath (Join-Path $root 'demos.json') -Raw | ConvertFrom-Json
$runDirectory = Join-Path $root "artifacts\launcher-rehearsal\$(Get-Date -Format 'yyyyMMdd-HHmmss-fff')"
[void](New-Item -ItemType Directory -Path $runDirectory -Force)
$results = [System.Collections.Generic.List[object]]::new()
$oldEnvironment = $env:ASPNETCORE_ENVIRONMENT
$oldDotnetEnvironment = $env:DOTNET_ENVIRONMENT
try {
    $env:ASPNETCORE_ENVIRONMENT = 'Development'
    $env:DOTNET_ENVIRONMENT = 'Development'
    foreach ($demo in $manifest.demos) {
        $dll = Join-Path $root "demos\$($demo.folder)\bin\Debug\net10.0\Orders.Demo$($demo.id).dll"
        if (-not (Test-Path -LiteralPath $dll)) { throw "Build the solution first; missing $dll." }
        $listener = [System.Net.Sockets.TcpListener]::new([System.Net.IPAddress]::Loopback, 0)
        $listener.Start()
        $port = $listener.LocalEndpoint.Port
        $listener.Stop()
        $database = Join-Path $runDirectory "demo-$($demo.id).db"
        $stdout = Join-Path $runDirectory "demo-$($demo.id).stdout.log"
        $stderr = Join-Path $runDirectory "demo-$($demo.id).stderr.log"
        $arguments = @("`"$dll`"", '--urls', "http://127.0.0.1:$port",
            '--Orders:Mode','fixed','--Orders:DatabasePath',"`"$database`"")
        $process = Start-Process dotnet -ArgumentList $arguments -WorkingDirectory $root `
            -PassThru -RedirectStandardOutput $stdout -RedirectStandardError $stderr
        try {
            $ready = $false
            $lastConnectionError = $null
            $deadline = [DateTime]::UtcNow.AddSeconds(30)
            do {
                if ($process.HasExited) { throw "Demo $($demo.id) exited before readiness. See $stderr and $stdout." }
                try {
                    $response = Invoke-WebRequest "http://127.0.0.1:$port/health" -TimeoutSec 2 -SkipHttpErrorCheck
                    if ([int]$response.StatusCode -eq 200) { $ready = $true; break }
                    throw "Unexpected health status $($response.StatusCode)."
                } catch [System.Net.Http.HttpRequestException] {
                    $lastConnectionError = $_.Exception.Message
                    Start-Sleep -Milliseconds 150
                }
            } while ([DateTime]::UtcNow -lt $deadline)
            if (-not $ready) { throw "Demo $($demo.id) not ready: $lastConnectionError. See $stderr." }
            foreach ($scenario in @('retry','payload','isolation','new-order')) {
                $evidence = & (Join-Path $PSScriptRoot 'Invoke-ApiScenario.ps1') -Port $port -Scenario $scenario
                $results.Add([pscustomobject]@{ demo=$demo.id; scenario=$scenario; result=$evidence.Result; port=$port })
            }
            Write-Host "Demo $($demo.id): four HTTP acceptance scenarios passed."
        } finally {
            if (-not $process.HasExited) {
                Stop-Process -Id $process.Id -ErrorAction Stop
                $process.WaitForExit()
            }
            $process.Dispose()
        }
    }
} finally {
    $env:ASPNETCORE_ENVIRONMENT = $oldEnvironment
    $env:DOTNET_ENVIRONMENT = $oldDotnetEnvironment
    $results | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $runDirectory 'results.json') -Encoding utf8
}
if ($results.Count -ne 36) { throw "Expected 36 launcher scenarios; observed $($results.Count)." }
Write-Host "Nine demo executables exercised successfully. Evidence: $runDirectory"
