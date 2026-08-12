# 경조사 명부 아이콘
# 세로형 봉투(부조 봉투) + 기재 줄. 가로형 봉투는 이메일 아이콘으로 읽혀서 피했다.
# 각 크기를 개별 렌더링한다. 축소 리샘플링보다 16px 에서 또렷하다.
Add-Type -AssemblyName System.Drawing

$OutIco  = $args[0]
$PrevDir = $args[1]
$sizes   = 16, 24, 32, 48, 64, 128, 256

function Get-Color([string]$hex) { [System.Drawing.ColorTranslator]::FromHtml($hex) }

function Get-RoundPath([float]$x, [float]$y, [float]$w, [float]$h, [float]$r) {
    $p = New-Object System.Drawing.Drawing2D.GraphicsPath
    $d = [float]($r * 2)
    if ($d -lt 1.5) {
        $p.AddRectangle((New-Object System.Drawing.RectangleF -ArgumentList $x, $y, $w, $h))
        return $p
    }
    $p.AddArc($x, $y, $d, $d, 180, 90)
    $p.AddArc($x + $w - $d, $y, $d, $d, 270, 90)
    $p.AddArc($x + $w - $d, $y + $h - $d, $d, $d, 0, 90)
    $p.AddArc($x, $y + $h - $d, $d, $d, 90, 90)
    $p.CloseFigure()
    return $p
}

function New-IconBitmap([int]$size) {
    $bmp = New-Object System.Drawing.Bitmap -ArgumentList $size, $size, ([System.Drawing.Imaging.PixelFormat]::Format32bppArgb)
    $g = [System.Drawing.Graphics]::FromImage($bmp)
    $g.SmoothingMode     = [System.Drawing.Drawing2D.SmoothingMode]::AntiAlias
    $g.PixelOffsetMode   = [System.Drawing.Drawing2D.PixelOffsetMode]::HighQuality
    $g.Clear([System.Drawing.Color]::Transparent)

    $s = $size / 256.0

    # ---------- 배경 ----------
    $bgRadius = [float]$(if ($size -le 24) { 32 * $s } elseif ($size -le 48) { 42 * $s } else { 52 * $s })
    $bgPath = Get-RoundPath 0 0 ([float]$size) ([float]$size) $bgRadius
    $bgRect = New-Object System.Drawing.RectangleF -ArgumentList ([float]0), ([float]0), ([float]$size), ([float]$size)
    $grad = New-Object System.Drawing.Drawing2D.LinearGradientBrush -ArgumentList $bgRect, (Get-Color "#5E97DB"), (Get-Color "#2F5C93"), ([float]60)
    $g.FillPath($grad, $bgPath)
    $grad.Dispose()

    # ---------- 세로형 봉투 ----------
    $ew = [float](116 * $s)
    $eh = [float](162 * $s)
    $ex = [float](($size - $ew) / 2)
    $ey = [float](($size - $eh) / 2)
    $envRadius = [float]$(if ($size -le 32) { 4 * $s } else { 10 * $s })

    $envPath = Get-RoundPath $ex $ey $ew $eh $envRadius
    $whiteBrush = New-Object System.Drawing.SolidBrush -ArgumentList (Get-Color "#FFFFFF")
    $g.FillPath($whiteBrush, $envPath)
    $whiteBrush.Dispose()

    # 덮개: 위쪽에 가로 띠. 봉투라는 신호를 주면서 16px 에서도 뭉개지지 않는다.
    $flapH = [float](46 * $s)
    $g.SetClip($envPath)
    $flapBrush = New-Object System.Drawing.SolidBrush -ArgumentList (Get-Color "#CFDFF2")
    $g.FillRectangle($flapBrush, $ex, $ey, $ew, $flapH)
    $flapBrush.Dispose()

    $linePen = New-Object System.Drawing.Pen -ArgumentList (Get-Color "#7DA3CC"), ([float][Math]::Max(1.0, 6 * $s))
    $g.DrawLine($linePen, $ex, ($ey + $flapH), ($ex + $ew), ($ey + $flapH))
    $linePen.Dispose()
    $g.ResetClip()

    # ---------- 기재 줄 (32px 이상에서만) ----------
    if ($size -ge 32) {
        $count = if ($size -le 48) { 2 } else { 3 }
        $pen = New-Object System.Drawing.Pen -ArgumentList (Get-Color "#93AFCB"), ([float][Math]::Max(1.0, 10 * $s))
        $pen.StartCap = [System.Drawing.Drawing2D.LineCap]::Round
        $pen.EndCap   = [System.Drawing.Drawing2D.LineCap]::Round
        $lx1 = [float]($ex + 22 * $s)
        $lx2 = [float]($ex + $ew - 22 * $s)
        $top = [float]($ey + $flapH + 34 * $s)
        $gap = [float](30 * $s)
        for ($i = 0; $i -lt $count; $i++) {
            $ly = [float]($top + $gap * $i)
            $end = if ($i -eq $count - 1) { [float]($lx1 + ($lx2 - $lx1) * 0.6) } else { $lx2 }
            $g.DrawLine($pen, $lx1, $ly, $end, $ly)
        }
        $pen.Dispose()
    }

    $envPath.Dispose(); $bgPath.Dispose(); $g.Dispose()
    return $bmp
}

# ---------- PNG 인코딩 후 ICO 조립 ----------
$entries = @()
foreach ($sz in $sizes) {
    $bmp = New-IconBitmap $sz
    if ($PrevDir) { $bmp.Save((Join-Path $PrevDir "final-$sz.png"), [System.Drawing.Imaging.ImageFormat]::Png) }
    $ms = New-Object System.IO.MemoryStream
    $bmp.Save($ms, [System.Drawing.Imaging.ImageFormat]::Png)
    $entries += , @($sz, $ms.ToArray())
    $ms.Dispose(); $bmp.Dispose()
}

$fs = [System.IO.File]::Create($OutIco)
$bw = New-Object System.IO.BinaryWriter -ArgumentList $fs
$bw.Write([UInt16]0); $bw.Write([UInt16]1); $bw.Write([UInt16]$entries.Count)
$offset = 6 + (16 * $entries.Count)
foreach ($e in $entries) {
    $sz = $e[0]; $data = $e[1]
    $b = [Byte]$(if ($sz -ge 256) { 0 } else { $sz })   # 256 은 0 으로 기록
    $bw.Write($b); $bw.Write($b); $bw.Write([Byte]0); $bw.Write([Byte]0)
    $bw.Write([UInt16]1); $bw.Write([UInt16]32)
    $bw.Write([UInt32]$data.Length); $bw.Write([UInt32]$offset)
    $offset += $data.Length
}
foreach ($e in $entries) { $bw.Write($e[1]) }
$bw.Flush(); $bw.Dispose(); $fs.Dispose()

"생성 완료: $OutIco  ($([math]::Round((Get-Item $OutIco).Length/1KB,1)) KB / 크기 $($entries.Count)종)"
