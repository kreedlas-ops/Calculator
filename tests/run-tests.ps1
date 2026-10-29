$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

$csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path $csc)) {
    $csc = Join-Path $env:WINDIR 'Microsoft.NET\Framework\v4.0.30319\csc.exe'
}

$exe = Join-Path $root 'tests\EngineTests.exe'
$arguments = @(
    '/nologo',
    '/target:exe',
    '/platform:anycpu',
    '/codepage:65001',
    '/langversion:5',
    ('/out:' + $exe),
    '/reference:System.dll',
    '/reference:System.Core.dll',
    (Join-Path $root 'src\CalculatorEngine.cs'),
    (Join-Path $root 'tests\EngineTests.cs')
)

& $csc $arguments
if ($LASTEXITCODE -ne 0) {
    throw "Test compilation failed with exit code $LASTEXITCODE."
}

& $exe
exit $LASTEXITCODE
