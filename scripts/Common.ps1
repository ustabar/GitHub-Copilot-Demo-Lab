Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'
$script:LabRoot = Split-Path -Parent $PSScriptRoot

function Get-LabRoot { return $script:LabRoot }

function Get-DemoDefinition {
    param([Parameter(Mandatory)][string]$Id)
    $manifest = Get-Content -LiteralPath (Join-Path $script:LabRoot 'demos.json') -Raw | ConvertFrom-Json
    $demo = @($manifest.demos | Where-Object id -eq $Id)
    if ($demo.Count -ne 1) { throw "Unknown demo '$Id'. Use 01..09." }
    return $demo[0]
}

function Get-LabGit {
    $command = Get-Command git -ErrorAction SilentlyContinue
    if ($command) { return $command.Source }
    $candidate = Join-Path $env:ProgramFiles 'Git\cmd\git.exe'
    if (Test-Path -LiteralPath $candidate) { return $candidate }
    throw 'Git was not found. Install Git or add it to PATH before this scenario.'
}

function Invoke-LabNative {
    param([Parameter(Mandatory)][string]$Command, [string[]]$Arguments = @())
    & $Command @Arguments
    if ($LASTEXITCODE -ne 0) { throw "$Command failed with exit code $LASTEXITCODE." }
}

function Copy-LabSource {
    param([Parameter(Mandatory)][string]$Source, [Parameter(Mandatory)][string]$Target)
    $excludedDirectories = @('.git','.runtime','artifacts','node_modules','bin','obj','dist','TestResults','test-results','playwright-report','.vs','.idea')
    $excludedNames = @('.npmrc','NuGet.Config','secrets.json','.DS_Store','Thumbs.db')
    $excludedExtensions = @('.db','.db-shm','.db-wal','.log','.zip','.7z','.tar','.gz','.dmp','.dump','.pfx','.p12','.pem','.key','.cer','.crt','.snk','.user')
    [void](New-Item -ItemType Directory -Path $Target -Force)
    foreach ($item in Get-ChildItem -LiteralPath $Source -Force) {
        if ($item.PSIsContainer) {
            if ($item.Name -notin $excludedDirectories) { Copy-LabSource $item.FullName (Join-Path $Target $item.Name) }
        } elseif ($item.Name -notin $excludedNames -and $item.Extension -notin $excludedExtensions -and $item.Name -notlike '.env*') {
            Copy-Item -LiteralPath $item.FullName -Destination (Join-Path $Target $item.Name)
        }
    }
}
