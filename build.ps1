param(
    [switch]$Run
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path

$candidates = @(
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'),
    (Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe')
)
$csc = $candidates | Where-Object { Test-Path $_ } | Select-Object -First 1
if (-not $csc) {
    throw 'The .NET Framework C# compiler (csc.exe) was not found.'
}

$sources = @(Get-ChildItem (Join-Path $root 'src') -Filter '*.cs' |
    Sort-Object Name |
    ForEach-Object { $_.FullName })

$references = @('System.dll', 'System.Core.dll', 'System.Drawing.dll', 'System.Windows.Forms.dll')
$exePath = Join-Path $root 'Calculator.exe'

$arguments = @(
    '/nologo',
    '/target:winexe',
    '/platform:anycpu',
    '/optimize+',
    '/codepage:65001',
    '/langversion:5',
    ('/win32icon:' + (Join-Path $root 'Calculator.ico')),
    ('/win32manifest:' + (Join-Path $root 'app.manifest')),
    ('/out:' + $exePath)
)
foreach ($reference in $references) {
    $arguments += ('/reference:' + $reference)
}
$arguments += $sources

& $csc $arguments
if ($LASTEXITCODE -ne 0) {
    throw "C# compilation failed with exit code $LASTEXITCODE."
}

Write-Host "Built $exePath"

if ($Run) {
    Start-Process -FilePath $exePath
}
