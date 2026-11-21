param([string]$name, [string]$keys = "")
$emu = Join-Path $PSScriptRoot "emu.exe"
$out = Join-Path (Split-Path $PSScriptRoot) "shots"
if ($keys) { & $emu keys $keys }
$dir = "$env:LOCALAPPDATA\DuckStation\screenshots"
$before = @(Get-ChildItem $dir -ErrorAction SilentlyContinue).Count
& $emu keys "F10:80"
for ($i = 0; $i -lt 30; $i++) { Start-Sleep -Milliseconds 200; if (@(Get-ChildItem $dir).Count -gt $before) { break } }
Start-Sleep -Milliseconds 300
$f = Get-ChildItem $dir | Sort-Object LastWriteTime | Select-Object -Last 1
Move-Item -LiteralPath $f.FullName (Join-Path $out "$name.png") -Force
