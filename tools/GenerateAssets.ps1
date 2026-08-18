$ErrorActionPreference = 'Stop'

Add-Type -AssemblyName System.Drawing

function New-AppIconBitmap {
    param([int]$Size)

    $bitmap = New-Object System.Drawing.Bitmap($Size, $Size, [System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.Clear([System.Drawing.Color]::Transparent)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $graphics.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $graphics.CompositingQuality = [System.Drawing.Drawing2D.CompositingQuality]::HighQuality

    $bounds = New-Object System.Drawing.RectangleF(($Size * 0.08), ($Size * 0.08), ($Size * 0.84), ($Size * 0.84))
    $gradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $bounds,
        [System.Drawing.Color]::FromArgb(255, 8, 211, 240),
        [System.Drawing.Color]::FromArgb(255, 109, 40, 217),
        45.0
    )
    $blend = New-Object System.Drawing.Drawing2D.ColorBlend
    $blend.Colors = @(
        [System.Drawing.Color]::FromArgb(255, 8, 211, 240),
        [System.Drawing.Color]::FromArgb(255, 37, 99, 235),
        [System.Drawing.Color]::FromArgb(255, 109, 40, 217)
    )
    $blend.Positions = @(0.0, 0.52, 1.0)
    $gradient.InterpolationColors = $blend

    $ringPen = New-Object System.Drawing.Pen($gradient, ($Size * 0.125))
    $ringPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $ringPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $ringRect = New-Object System.Drawing.RectangleF(($Size * 0.18), ($Size * 0.18), ($Size * 0.64), ($Size * 0.64))
    $graphics.DrawArc($ringPen, $ringRect, 318.0, 264.0)

    $powerPen = New-Object System.Drawing.Pen($gradient, ($Size * 0.12))
    $powerPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $powerPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($powerPen, ($Size * 0.50), ($Size * 0.13), ($Size * 0.50), ($Size * 0.35))

    $handPen = New-Object System.Drawing.Pen($gradient, ($Size * 0.055))
    $handPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($handPen, ($Size * 0.50), ($Size * 0.52), ($Size * 0.40), ($Size * 0.46))
    $graphics.DrawLine($handPen, ($Size * 0.50), ($Size * 0.52), ($Size * 0.63), ($Size * 0.40))

    $tickPen = New-Object System.Drawing.Pen($gradient, ($Size * 0.035))
    $tickPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $tickPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $graphics.DrawLine($tickPen, ($Size * 0.29), ($Size * 0.52), ($Size * 0.33), ($Size * 0.52))
    $graphics.DrawLine($tickPen, ($Size * 0.67), ($Size * 0.52), ($Size * 0.71), ($Size * 0.52))
    $graphics.DrawLine($tickPen, ($Size * 0.50), ($Size * 0.69), ($Size * 0.50), ($Size * 0.73))

    $centerSize = $Size * 0.085
    $graphics.FillEllipse($gradient, ($Size * 0.50 - $centerSize / 2), ($Size * 0.52 - $centerSize / 2), $centerSize, $centerSize)

    $tickPen.Dispose()
    $handPen.Dispose()
    $powerPen.Dispose()
    $ringPen.Dispose()
    $gradient.Dispose()
    $graphics.Dispose()
    return $bitmap
}

function Write-MultiSizeIcon {
    param(
        [string]$Path,
        [int[]]$Sizes
    )

    $images = @()
    foreach ($size in $Sizes) {
        $bitmap = New-AppIconBitmap -Size $size
        $stream = New-Object System.IO.MemoryStream
        $bitmap.Save($stream, [System.Drawing.Imaging.ImageFormat]::Png)
        $bitmap.Dispose()
        $images += ,$stream.ToArray()
        $stream.Dispose()
    }

    $file = [System.IO.File]::Open($Path, [System.IO.FileMode]::Create)
    $writer = New-Object System.IO.BinaryWriter($file)
    $writer.Write([uint16]0)
    $writer.Write([uint16]1)
    $writer.Write([uint16]$Sizes.Count)
    $offset = 6 + (16 * $Sizes.Count)

    for ($index = 0; $index -lt $Sizes.Count; $index++) {
        $sizeByte = if ($Sizes[$index] -eq 256) { 0 } else { $Sizes[$index] }
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]$sizeByte)
        $writer.Write([byte]0)
        $writer.Write([byte]0)
        $writer.Write([uint16]1)
        $writer.Write([uint16]32)
        $writer.Write([uint32]$images[$index].Length)
        $writer.Write([uint32]$offset)
        $offset += $images[$index].Length
    }

    foreach ($bytes in $images) {
        $writer.Write($bytes)
    }

    $writer.Dispose()
    $file.Dispose()
}

$assetDirectory = Join-Path $PSScriptRoot '..\assets'
New-Item -ItemType Directory -Force -Path $assetDirectory | Out-Null

$master = New-AppIconBitmap -Size 1024
$master.Save((Join-Path $assetDirectory 'app-icon.png'), [System.Drawing.Imaging.ImageFormat]::Png)
$master.Dispose()

Write-MultiSizeIcon -Path (Join-Path $assetDirectory 'app.ico') -Sizes @(256, 64, 48, 32, 24, 16)
Write-Host 'Generated assets/app-icon.png and assets/app.ico'
