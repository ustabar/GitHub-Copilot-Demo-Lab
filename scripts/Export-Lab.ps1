#requires -Version 7.2
[CmdletBinding()]
param()
. (Join-Path $PSScriptRoot 'Common.ps1')
$root = Get-LabRoot
$stage = Join-Path $root "artifacts\export-$([guid]::NewGuid().ToString('N'))"
$content = Join-Path $stage 'GitHub-Copilot-Demo-Lab'
Copy-LabSource $root $content
$parent = Split-Path -Parent $root
$destination = Join-Path $parent 'GitHub-Copilot-Demo-Lab.zip'
$index = 2
while (Test-Path -LiteralPath $destination) {
    $destination = Join-Path $parent "GitHub-Copilot-Demo-Lab-$index.zip"
    $index++
}
try {
    [System.IO.Compression.ZipFile]::CreateFromDirectory($stage, $destination)
    $archive = [System.IO.Compression.ZipFile]::OpenRead($destination)
    try {
        $names = @($archive.Entries | ForEach-Object FullName)
        foreach ($demo in (Get-Content (Join-Path $root 'demos.json') -Raw | ConvertFrom-Json).demos) {
            if ("GitHub-Copilot-Demo-Lab/demos/$($demo.folder)/README.md" -notin $names) {
                throw "Archive missing guide $($demo.folder)."
            }
        }
        if ($names | Where-Object { $_ -match '/(node_modules|bin|obj|\.runtime|\.git)/' }) {
            throw 'Archive includes non-source runtime data.'
        }
    } finally { $archive.Dispose() }
} finally {
    Remove-Item -LiteralPath $stage -Recurse -Force
}
Write-Output $destination
