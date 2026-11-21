param([string]$key, [int]$x, [int]$y, [int]$r, [int]$g, [int]$b, [int]$tries = 15, [int]$waitms = 2500)
# press $key repeatedly until screenshot pixel (x,y) ~= (r,g,b)
Add-Type -AssemblyName System.Drawing
$shot = Join-Path $PSScriptRoot "shot.ps1"
$emu = Join-Path $PSScriptRoot "emu.exe"
$out = Join-Path (Split-Path $PSScriptRoot) "shots\until.png"
for ($i = 0; $i -lt $tries; $i++) {
    & $shot until
    $im = [Drawing.Bitmap]::FromFile($out)
    $c = $im.GetPixel([int]($x * $im.Width / 792), [int]($y * $im.Height / 455)); $im.Dispose()
    if ([Math]::Abs($c.R - $r) -lt 24 -and [Math]::Abs($c.G - $g) -lt 24 -and [Math]::Abs($c.B - $b) -lt 24) { "ok after $i"; return }
    if ($key) { & $emu keys "$($key):100" }
    Start-Sleep -Milliseconds $waitms
}
"fail"

