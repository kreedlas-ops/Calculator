$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

# Builds Calculator.ico from assets\app-icon.png.
# The source picture is cropped to its visible content, centered on a square
# transparent canvas and rendered at every size Windows asks for.

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)
$sourcePath = Join-Path $root 'assets\app-icon.png'
$icoPath = Join-Path $root 'Calculator.ico'

if (-not (Test-Path $sourcePath)) {
    throw "Source picture not found: $sourcePath"
}

$source = [System.Drawing.Bitmap]::FromFile($sourcePath)

# Find the visible content, ignoring the transparent border.
$minX = $source.Width
$minY = $source.Height
$maxX = -1
$maxY = -1
for ($y = 0; $y -lt $source.Height; $y++) {
    for ($x = 0; $x -lt $source.Width; $x++) {
        if ($source.GetPixel($x, $y).A -gt 8) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

if ($maxX -lt 0) {
    throw 'The source picture is fully transparent.'
}

$cropRect = New-Object System.Drawing.Rectangle($minX, $minY, ($maxX - $minX + 1), ($maxY - $minY + 1))

# Square master canvas at high resolution, content centered with a small margin.
$masterSize = 1024
$margin = [int]($masterSize * 0.03)
$available = $masterSize - ($margin * 2)
$scale = [Math]::Min($available / $cropRect.Width, $available / $cropRect.Height)
$drawWidth = [int][Math]::Round($cropRect.Width * $scale)
$drawHeight = [int][Math]::Round($cropRect.Height * $scale)
$drawX = [int](($masterSize - $drawWidth) / 2)
$drawY = [int](($masterSize - $drawHeight) / 2)

$master = New-Object System.Drawing.Bitmap($masterSize, $masterSize, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
$masterGraphics = [System.Drawing.Graphics]::FromImage($master)
$masterGraphics.Clear([System.Drawing.Color]::Transparent)
$masterGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$masterGraphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
$masterGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$masterGraphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
$masterGraphics.DrawImage($source, (New-Object System.Drawing.Rectangle($drawX, $drawY, $drawWidth, $drawHeight)))
$masterGraphics.Dispose()
$source.Dispose()

function Get-IconBitmap {
    param([System.Drawing.Bitmap]$Master, [int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.Clear([System.Drawing.Color]::Transparent)
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality
    $g.DrawImage($Master, (New-Object System.Drawing.Rectangle(0, 0, $Size, $Size)))
    $g.Dispose()
    return $bmp
}

function Get-IconDib {
    param([System.Drawing.Bitmap]$Bitmap)

    $size = $Bitmap.Width
    $rect = New-Object System.Drawing.Rectangle(0, 0, $size, $size)
    $data = $Bitmap.LockBits($rect, [System.Drawing.Imaging.ImageLockMode]::ReadOnly, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $stride = $data.Stride
    $pixels = New-Object byte[] ($stride * $size)
    [System.Runtime.InteropServices.Marshal]::Copy($data.Scan0, $pixels, 0, $pixels.Length)
    $Bitmap.UnlockBits($data)

    $maskStride = [int]([Math]::Floor(($size + 31) / 32) * 4)
    $mask = New-Object byte[] ($maskStride * $size)
    $pixelBytes = $size * $size * 4

    $stream = New-Object System.IO.MemoryStream
    $writer = New-Object System.IO.BinaryWriter($stream)

    # BITMAPINFOHEADER: height is doubled because the DIB also carries the AND mask.
    $writer.Write([int]40)
    $writer.Write([int]$size)
    $writer.Write([int]($size * 2))
    $writer.Write([int16]1)
    $writer.Write([int16]32)
    $writer.Write([int]0)
    $writer.Write([int]$pixelBytes)
    $writer.Write([int]0)
    $writer.Write([int]0)
    $writer.Write([int]0)
    $writer.Write([int]0)

    # XOR bitmap, bottom-up.
    for ($y = $size - 1; $y -ge 0; $y--) {
        $writer.Write($pixels, $y * $stride, $size * 4)
    }

    # AND mask: fully opaque, the alpha channel does the real work.
    $writer.Write($mask, 0, $mask.Length)
    $writer.Flush()

    $bytes = $stream.ToArray()
    $writer.Dispose()
    $stream.Dispose()
    return $bytes
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($size in $sizes) {
    $bitmap = Get-IconBitmap -Master $master -Size $size
    $dib = Get-IconDib -Bitmap $bitmap
    $images += , @($size, $dib)
    $bitmap.Dispose()
}

$file = [System.IO.File]::Create($icoPath)
$writer = New-Object System.IO.BinaryWriter($file)
$writer.Write([uint16]0)
$writer.Write([uint16]1)
$writer.Write([uint16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
foreach ($entry in $images) {
    $size = [int]$entry[0]
    $dib = [byte[]]$entry[1]
    $dimension = if ($size -ge 256) { 0 } else { $size }
    $writer.Write([byte]$dimension)
    $writer.Write([byte]$dimension)
    $writer.Write([byte]0)
    $writer.Write([byte]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]32)
    $writer.Write([uint32]$dib.Length)
    $writer.Write([uint32]$offset)
    $offset += $dib.Length
}
foreach ($entry in $images) {
    $writer.Write([byte[]]$entry[1])
}
$writer.Flush()
$writer.Dispose()
$file.Dispose()

Write-Host "Created $icoPath"

# Preview sheet, handy for checking how the icon reads at small sizes.
$preview = New-Object System.Drawing.Bitmap(600, 320)
$previewGraphics = [System.Drawing.Graphics]::FromImage($preview)
$previewGraphics.Clear([System.Drawing.Color]::FromArgb(255, 31, 31, 41))
$previewGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$labelFont = New-Object System.Drawing.Font('Segoe UI', 8)
$x = 18
foreach ($entry in $images) {
    $size = [int]$entry[0]
    $bitmap = Get-IconBitmap -Master $master -Size $size
    $previewGraphics.DrawImage($bitmap, $x, 24, $size, $size)
    $previewGraphics.DrawString([string]$size, $labelFont, [System.Drawing.Brushes]::White, $x, 24 + $size + 6)
    $bitmap.Dispose()
    $x += $size + 18
}
$previewGraphics.Dispose()
$previewPath = Join-Path $env:TEMP 'calc-icon-preview.png'
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
$labelFont.Dispose()
$master.Dispose()
Write-Host "Preview $previewPath"
