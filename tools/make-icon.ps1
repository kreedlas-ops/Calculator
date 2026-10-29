$ErrorActionPreference = 'Stop'
Add-Type -AssemblyName System.Drawing

$root = Split-Path -Parent (Split-Path -Parent $MyInvocation.MyCommand.Path)

function New-RoundedPath {
    param(
        [System.Drawing.RectangleF]$Rect,
        [float]$Radius
    )
    $path = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $Radius * 2
    if ($d -gt $Rect.Width) { $d = $Rect.Width }
    if ($d -gt $Rect.Height) { $d = $Rect.Height }
    $path.AddArc($Rect.X, $Rect.Y, $d, $d, 180, 90)
    $path.AddArc($Rect.Right - $d, $Rect.Y, $d, $d, 270, 90)
    $path.AddArc($Rect.Right - $d, $Rect.Bottom - $d, $d, $d, 0, 90)
    $path.AddArc($Rect.X, $Rect.Bottom - $d, $d, $d, 90, 90)
    $path.CloseFigure()
    return $path
}

function New-IconBitmap {
    param([int]$Size)

    $bmp = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.Clear([System.Drawing.Color]::Transparent)

    $unit = $Size / 256.0

    $bgRect = New-Object System.Drawing.RectangleF(0, 0, $Size, $Size)
    $bgPath = New-RoundedPath -Rect $bgRect -Radius (56 * $unit)
    $bgBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bgRect,
        [System.Drawing.Color]::FromArgb(255, 104, 211, 255),
        [System.Drawing.Color]::FromArgb(255, 40, 148, 214),
        45.0)
    $g.FillPath($bgBrush, $bgPath)

    $bodyRect = New-Object System.Drawing.RectangleF((60 * $unit), (38 * $unit), (136 * $unit), (180 * $unit))
    $bodyPath = New-RoundedPath -Rect $bodyRect -Radius (22 * $unit)
    $bodyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 247, 251, 255))
    $g.FillPath($bodyBrush, $bodyPath)

    $screenRect = New-Object System.Drawing.RectangleF((73 * $unit), (51 * $unit), (110 * $unit), (31 * $unit))
    $screenPath = New-RoundedPath -Rect $screenRect -Radius (6 * $unit)
    $screenBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 13, 52, 74))
    $g.FillPath($screenBrush, $screenPath)

    $keyBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(255, 27, 84, 112))
    foreach ($ky in @(104, 138, 172)) {
        foreach ($kx in @(84, 128, 172)) {
            $keyRect = New-Object System.Drawing.RectangleF((($kx - 13) * $unit), (($ky - 9) * $unit), (26 * $unit), (18 * $unit))
            $keyPath = New-RoundedPath -Rect $keyRect -Radius (4 * $unit)
            $g.FillPath($keyBrush, $keyPath)
            $keyPath.Dispose()
        }
    }

    $keyBrush.Dispose()
    $screenBrush.Dispose()
    $screenPath.Dispose()
    $bodyBrush.Dispose()
    $bodyPath.Dispose()
    $bgBrush.Dispose()
    $bgPath.Dispose()
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
    $total = 40 + $pixelBytes + $mask.Length

    $ms = New-Object System.IO.MemoryStream
    $w = New-Object System.IO.BinaryWriter($ms)

    # BITMAPINFOHEADER
    $w.Write([int]40)
    $w.Write([int]$size)
    $w.Write([int]($size * 2))
    $w.Write([int16]1)
    $w.Write([int16]32)
    $w.Write([int]0)
    $w.Write([int]$pixelBytes)
    $w.Write([int]0)
    $w.Write([int]0)
    $w.Write([int]0)
    $w.Write([int]0)

    # XOR bitmap, bottom-up
    for ($y = $size - 1; $y -ge 0; $y--) {
        $w.Write($pixels, $y * $stride, $size * 4)
    }
    $w.Write($mask, 0, $mask.Length)
    $w.Flush()

    $bytes = $ms.ToArray()
    $w.Dispose()
    $ms.Dispose()
    return $bytes
}

$sizes = @(16, 20, 24, 32, 40, 48, 64, 128, 256)
$images = @()
foreach ($s in $sizes) {
    $bmp = New-IconBitmap -Size $s
    $dib = Get-IconDib -Bitmap $bmp
    $images += , @($s, $dib)
    $bmp.Dispose()
}

$icoPath = Join-Path $root 'Calculator.ico'
$fs = [System.IO.File]::Create($icoPath)
$bw = New-Object System.IO.BinaryWriter($fs)
$bw.Write([uint16]0)
$bw.Write([uint16]1)
$bw.Write([uint16]$sizes.Count)

$offset = 6 + (16 * $sizes.Count)
foreach ($entry in $images) {
    $s = [int]$entry[0]
    $dib = [byte[]]$entry[1]
    $dim = if ($s -ge 256) { 0 } else { $s }
    $bw.Write([byte]$dim)
    $bw.Write([byte]$dim)
    $bw.Write([byte]0)
    $bw.Write([byte]0)
    $bw.Write([uint16]1)
    $bw.Write([uint16]32)
    $bw.Write([uint32]$dib.Length)
    $bw.Write([uint32]$offset)
    $offset += $dib.Length
}
foreach ($entry in $images) {
    $bw.Write([byte[]]$entry[1])
}
$bw.Flush()
$bw.Dispose()
$fs.Dispose()

Write-Host "Created $icoPath"

# Preview sheet, useful when checking the icon by eye.
$preview = New-Object System.Drawing.Bitmap(560, 300)
$pg = [System.Drawing.Graphics]::FromImage($preview)
$pg.Clear([System.Drawing.Color]::FromArgb(255, 31, 31, 41))
$pg.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
$x = 18
foreach ($entry in $images) {
    $s = [int]$entry[0]
    $bmp = New-IconBitmap -Size $s
    $pg.DrawImage($bmp, $x, 24, $s, $s)
    $labelFont = New-Object System.Drawing.Font('Segoe UI', 8)
    $pg.DrawString([string]$s, $labelFont, [System.Drawing.Brushes]::White, $x, 24 + $s + 6)
    $labelFont.Dispose()
    $bmp.Dispose()
    $x += $s + 18
}
$pg.Dispose()
$previewPath = Join-Path $env:TEMP 'calc-icon-preview.png'
$preview.Save($previewPath, [System.Drawing.Imaging.ImageFormat]::Png)
$preview.Dispose()
Write-Host "Preview $previewPath"
