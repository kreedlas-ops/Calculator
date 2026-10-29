param(
    [string]$Sequence = '1,0,ADD,1,RETURN',
    [string]$Output = ''
)

# Opens Calculator.exe, types the given key sequence, and saves a picture of the window.
# Keys are posted directly to the calculator, so no window focus is needed.
# Sequence names: digits, ADD, SUBTRACT, MULTIPLY, DIVIDE, DECIMAL, RETURN, BACK, ESCAPE, DELETE.

$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing
Add-Type -TypeDefinition @'
using System;
using System.Runtime.InteropServices;
public static class CalcShotWin32
{
    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left; public int Top; public int Right; public int Bottom; }

    [DllImport("dwmapi.dll")]
    public static extern int DwmGetWindowAttribute(IntPtr hwnd, int attribute, out RECT value, int size);

    [DllImport("user32.dll")]
    public static extern bool PrintWindow(IntPtr hwnd, IntPtr hdc, uint flags);

    [DllImport("user32.dll")]
    public static extern bool PostMessage(IntPtr hwnd, uint message, IntPtr wParam, IntPtr lParam);
}
'@

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$keys = $Sequence -split ',' | ForEach-Object { $_.Trim() } | Where-Object { $_ -ne '' }

if ([string]::IsNullOrWhiteSpace($Output)) {
    $Output = Join-Path $env:TEMP 'calculator.png'
}

$outputDirectory = Split-Path -Parent $Output
New-Item -ItemType Directory -Force -Path $outputDirectory | Out-Null

$virtualKeys = @{
    '0' = 0x30; '1' = 0x31; '2' = 0x32; '3' = 0x33; '4' = 0x34; '5' = 0x35
    '6' = 0x36; '7' = 0x37; '8' = 0x38; '9' = 0x39
    'ADD' = 0x6B; 'SUBTRACT' = 0x6D; 'MULTIPLY' = 0x6A; 'DIVIDE' = 0x6F
    'DECIMAL' = 0x6E; 'RETURN' = 0x0D; 'BACK' = 0x08; 'ESCAPE' = 0x1B; 'DELETE' = 0x2E
}

$process = Start-Process -FilePath (Join-Path $root 'Calculator.exe') -PassThru
$handle = [IntPtr]::Zero
for ($i = 0; $i -lt 60; $i++) {
    Start-Sleep -Milliseconds 100
    $process.Refresh()
    if ($process.MainWindowHandle -ne [IntPtr]::Zero) {
        $handle = $process.MainWindowHandle
        break
    }
}

if ($handle -eq [IntPtr]::Zero) {
    throw 'Calculator window did not appear.'
}

Start-Sleep -Milliseconds 700

foreach ($key in $keys) {
    if (-not $virtualKeys.ContainsKey($key)) {
        throw "Unknown key: $key"
    }

    $virtualKey = $virtualKeys[$key]
    $down = [IntPtr](1 -bor ($virtualKey -shl 16))
    $up = [IntPtr](0xC0000001 -bor ($virtualKey -shl 16))
    [CalcShotWin32]::PostMessage($handle, 0x0100, [IntPtr]$virtualKey, $down) | Out-Null
    [CalcShotWin32]::PostMessage($handle, 0x0101, [IntPtr]$virtualKey, $up) | Out-Null
    Start-Sleep -Milliseconds 70
}

Start-Sleep -Milliseconds 400

$rect = New-Object CalcShotWin32+RECT
[CalcShotWin32]::DwmGetWindowAttribute($handle, 9, [ref]$rect, 16) | Out-Null
$width = $rect.Right - $rect.Left
$height = $rect.Bottom - $rect.Top
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$hdc = $graphics.GetHdc()
[CalcShotWin32]::PrintWindow($handle, $hdc, 2) | Out-Null
$graphics.ReleaseHdc($hdc)
$graphics.Dispose()
$bitmap.Save($Output, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()
Write-Host "Saved $Output (${width}x${height})"

$process.CloseMainWindow() | Out-Null
if (-not $process.WaitForExit(3000)) {
    $process.Kill()
}
