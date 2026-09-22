# ========================================================
# CleanDesk Icon Generator & Shortcut Linker
# Generates multi-resolution Windows .ico with Fluent 2 design
# ========================================================

Add-Type -AssemblyName System.Drawing

$projectDir = Split-Path -Parent $MyInvocation.MyCommand.Path
if ([string]::IsNullOrEmpty($projectDir)) {
    $projectDir = "C:\Users\FaberOs\Documents\Projects\CSharp\clean-desk-widget"
}

$icoPath = Join-Path $projectDir "app.ico"
$binReleaseDir = Join-Path $projectDir "bin\Release"
if (-not (Test-Path $binReleaseDir)) {
    New-Item -ItemType Directory -Path $binReleaseDir -Force | Out-Null
}
$icoBinPath = Join-Path $binReleaseDir "app.ico"

if (Test-Path $icoPath) {
    Write-Host "[*] app.ico personalizado detectado en el codigo fuente. Preservando icono del usuario..." -ForegroundColor Green
    Copy-Item -Path $icoPath -Destination $icoBinPath -Force -ErrorAction SilentlyContinue

    $desktopPath = [Environment]::GetFolderPath("Desktop")
    $shortcutPath = Join-Path $desktopPath "CleanDesk.lnk"
    $targetExe = Join-Path $projectDir "CleanDesk.exe"
    try {
        $ws = New-Object -ComObject WScript.Shell
        $shortcut = $ws.CreateShortcut($shortcutPath)
        $shortcut.TargetPath = $targetExe
        $shortcut.WorkingDirectory = $projectDir
        $shortcut.IconLocation = "$targetExe,0"
        $shortcut.Description = "CleanDesk - Limpiador y Gestor Dev"
        $shortcut.Save()
        Write-Host "[OK] Acceso directo en el Escritorio actualizado con el icono personalizado." -ForegroundColor Green
    } catch { }

    exit 0
}

Write-Host "[*] Generando icono CleanDesk en: $icoPath" -ForegroundColor Cyan

function New-Pt([float]$x, [float]$y) {
    return (New-Object System.Drawing.PointF -ArgumentList $x, $y)
}

$sizes = @(16, 32, 48, 64, 128, 256)
$iconImages = @()

foreach ($size in $sizes) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
    $g.PixelOffsetMode = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality

    $pad = [float]($size * 0.05)
    $rectSize = [float]($size - ($pad * 2))
    $radius = [float]($size * 0.24)

    # 1. Fondo de Esquinas Redondeadas (Squircle) con degradado Fluent Blue
    $gp = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = $radius * 2
    $rect = New-Object System.Drawing.RectangleF -ArgumentList $pad, $pad, $rectSize, $rectSize
    $gp.AddArc($rect.X, $rect.Y, $d, $d, 180, 90)
    $gp.AddArc($rect.Right - $d, $rect.Y, $d, $d, 270, 90)
    $gp.AddArc($rect.Right - $d, $rect.Bottom - $d, $d, $d, 0, 90)
    $gp.AddArc($rect.X, $rect.Bottom - $d, $d, $d, 90, 90)
    $gp.CloseFigure()

    $p1 = New-Pt 0 0
    $p2 = New-Pt $size $size
    $c1 = [System.Drawing.Color]::FromArgb(255, 30, 114, 254)   # #1E72FE Fluent Accent
    $c2 = [System.Drawing.Color]::FromArgb(255, 10, 50, 160)    # #0A32A0 Deep Ocean Blue
    $brush = New-Object System.Drawing.Drawing2D.LinearGradientBrush -ArgumentList $p1, $p2, $c1, $c2
    $g.FillPath($brush, $gp)
    $brush.Dispose()

    # Borde Sutil Fluent
    $borderPen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(100, 255, 255, 255)), ([float]([Math]::Max(1.0, $size * 0.03)))
    $g.DrawPath($borderPen, $gp)
    $borderPen.Dispose()
    $gp.Dispose()

    # 2. Mango de la Escoba (Largo, blanco, diagonal a 45 grados)
    $handleThickness = [float]([Math]::Max(2.2, $size * 0.095))
    $handlePen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(255, 255, 255, 255)), $handleThickness
    $handlePen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $handlePen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($handlePen, [float]($size * 0.74), [float]($size * 0.24), [float]($size * 0.44), [float]($size * 0.54))
    $handlePen.Dispose()

    # 3. Collar Dorado (Ferrule / Abrazadera de unión)
    $collarThickness = [float]([Math]::Max(2.6, $size * 0.125))
    $collarPen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(255, 245, 158, 11)), $collarThickness # #F59E0B
    $collarPen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
    $collarPen.EndCap = [System.Drawing.Drawing2D.LineCap]::Round
    $g.DrawLine($collarPen, [float]($size * 0.49), [float]($size * 0.49), [float]($size * 0.39), [float]($size * 0.59))
    $collarPen.Dispose()

    # 4. Cabezal de Cerdas en Abanico (Bristles Fan)
    $bristlesBrush = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $bristlePts = [System.Drawing.PointF[]]@(
        (New-Pt ($size * 0.47) ($size * 0.51)),
        (New-Pt ($size * 0.39) ($size * 0.59)),
        (New-Pt ($size * 0.16) ($size * 0.74)),
        (New-Pt ($size * 0.22) ($size * 0.84)),
        (New-Pt ($size * 0.32) ($size * 0.84)),
        (New-Pt ($size * 0.42) ($size * 0.76)),
        (New-Pt ($size * 0.47) ($size * 0.65))
    )
    $g.FillPolygon($bristlesBrush, $bristlePts)
    $bristlesBrush.Dispose()

    # Ranuras de separación de cerdas (Definen claramente la escoba)
    $groovePen = New-Object System.Drawing.Pen -ArgumentList ([System.Drawing.Color]::FromArgb(200, 10, 50, 160)), ([float]([Math]::Max(1.0, $size * 0.035)))
    $g.DrawLine($groovePen, [float]($size * 0.42), [float]($size * 0.56), [float]($size * 0.22), [float]($size * 0.82))
    $g.DrawLine($groovePen, [float]($size * 0.44), [float]($size * 0.54), [float]($size * 0.30), [float]($size * 0.83))
    $g.DrawLine($groovePen, [float]($size * 0.46), [float]($size * 0.52), [float]($size * 0.38), [float]($size * 0.77))
    $groovePen.Dispose()

    # 5. Destello Mágico Primario (4-Point Diamond Sparkle) en esquina superior derecha
    $sparkleBrush = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(255, 74, 222, 128)) # #4ADE80 Emerald
    $cx = [float]($size * 0.78)
    $cy = [float]($size * 0.22)
    $spR = [float]($size * 0.14)
    $spIn = [float]($size * 0.035)
    $spPts = [System.Drawing.PointF[]]@(
        (New-Pt ($cx) ($cy - $spR)),
        (New-Pt ($cx + $spIn) ($cy - $spIn)),
        (New-Pt ($cx + $spR) ($cy)),
        (New-Pt ($cx + $spIn) ($cy + $spIn)),
        (New-Pt ($cx) ($cy + $spR)),
        (New-Pt ($cx - $spIn) ($cy + $spIn)),
        (New-Pt ($cx - $spR) ($cy)),
        (New-Pt ($cx - $spIn) ($cy - $spIn))
    )
    $g.FillPolygon($sparkleBrush, $spPts)
    $sparkleBrush.Dispose()

    # Núcleo blanco del destello
    $coreBrush = New-Object System.Drawing.SolidBrush -ArgumentList ([System.Drawing.Color]::FromArgb(255, 255, 255, 255))
    $coreR = [float]($size * 0.04)
    $g.FillEllipse($coreBrush, [float]($cx - $coreR), [float]($cy - $coreR), [float]($coreR * 2), [float]($coreR * 2))

    # 6. Destello Mágico Secundario (Blanco puro suave en zona izquierda)
    $cx2 = [float]($size * 0.24)
    $cy2 = [float]($size * 0.34)
    $spR2 = [float]($size * 0.08)
    $spIn2 = [float]($size * 0.02)
    $spPts2 = [System.Drawing.PointF[]]@(
        (New-Pt ($cx2) ($cy2 - $spR2)),
        (New-Pt ($cx2 + $spIn2) ($cy2 - $spIn2)),
        (New-Pt ($cx2 + $spR2) ($cy2)),
        (New-Pt ($cx2 + $spIn2) ($cy2 + $spIn2)),
        (New-Pt ($cx2) ($cy2 + $spR2)),
        (New-Pt ($cx2 - $spIn2) ($cy2 + $spIn2)),
        (New-Pt ($cx2 - $spR2) ($cy2)),
        (New-Pt ($cx2 - $spIn2) ($cy2 - $spIn2))
    )
    $g.FillPolygon($coreBrush, $spPts2)
    $coreBrush.Dispose()

    $g.Dispose()

    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $iconImages += ,@($size, $ms.ToArray())
    $bmp.Dispose()
    $ms.Dispose()
}

# Escribir estructura de archivo .ico estándar compatible
function Write-IcoFile([string]$path, $images) {
    $fs = New-Object System.IO.FileStream($path, [System.IO.FileMode]::Create, [System.IO.FileAccess]::Write)
    $bw = New-Object System.IO.BinaryWriter($fs)

    # ICONDIR
    $bw.Write([uint16]0) # idReserved
    $bw.Write([uint16]1) # idType (1 = icon)
    $bw.Write([uint16]$images.Count)

    $offset = 6 + (16 * $images.Count)
    foreach ($img in $images) {
        $sz = $img[0]
        $bytes = $img[1]
        $w = if ($sz -ge 256) { 0 } else { $sz }
        $bw.Write([byte]$w)              # bWidth
        $bw.Write([byte]$w)              # bHeight
        $bw.Write([byte]0)               # bColorCount
        $bw.Write([byte]0)               # bReserved
        $bw.Write([uint16]1)             # wPlanes
        $bw.Write([uint16]32)            # wBitCount
        $bw.Write([uint32]$bytes.Length) # dwBytesInRes
        $bw.Write([uint32]$offset)       # dwImageOffset
        $offset += $bytes.Length
    }

    foreach ($img in $images) {
        $bytes = $img[1]
        $bw.Write($bytes)
    }

    $bw.Close()
    $fs.Close()
}

Write-IcoFile $icoPath $iconImages
Write-IcoFile $icoBinPath $iconImages
Write-Host "[OK] Icono generado exitosamente en resoluciones 16, 32, 48, 64, 128, 256 px." -ForegroundColor Green

# 6. Actualizar Acceso Directo del Escritorio si existe
$desktopPath = [Environment]::GetFolderPath("Desktop")
$shortcutPath = Join-Path $desktopPath "CleanDesk.lnk"
$targetExe = Join-Path $projectDir "CleanDesk.exe"

try {
    $ws = New-Object -ComObject WScript.Shell
    $shortcut = $ws.CreateShortcut($shortcutPath)
    $shortcut.TargetPath = $targetExe
    $shortcut.WorkingDirectory = $projectDir
    $shortcut.IconLocation = "$targetExe,0"
    $shortcut.Description = "CleanDesk - Limpiador y Gestor Dev"
    $shortcut.Save()
    Write-Host "[OK] Acceso directo en el Escritorio actualizado con el icono nativo." -ForegroundColor Green
} catch {
    Write-Host "[!] No se pudo vincular directamente el acceso directo del escritorio: $($_.Message)" -ForegroundColor Yellow
}
