
$assetsDir = Join-Path $PSScriptRoot "Assets"
if (!(Test-Path $assetsDir)) { New-Item -ItemType Directory $assetsDir }

# Start Sound
$SampleRate = 44100
$DurMs = 150
$Samples = [int]($SampleRate * ($DurMs / 1000))
$dataLen = $Samples * 2
$buffer = New-Object byte[] (44 + $dataLen)
[System.Text.Encoding]::ASCII.GetBytes("RIFF").CopyTo($buffer, 0)
[BitConverter]::GetBytes([int](36 + $dataLen)).CopyTo($buffer, 4)
[System.Text.Encoding]::ASCII.GetBytes("WAVE").CopyTo($buffer, 8)
[System.Text.Encoding]::ASCII.GetBytes("fmt ").CopyTo($buffer, 12)
[BitConverter]::GetBytes([int]16).CopyTo($buffer, 16)
[BitConverter]::GetBytes([short]1).CopyTo($buffer, 20)
[BitConverter]::GetBytes([short]1).CopyTo($buffer, 22)
[BitConverter]::GetBytes([int]$SampleRate).CopyTo($buffer, 24)
[BitConverter]::GetBytes([int]($SampleRate * 2)).CopyTo($buffer, 28)
[BitConverter]::GetBytes([short]2).CopyTo($buffer, 32)
[BitConverter]::GetBytes([short]16).CopyTo($buffer, 34)
[System.Text.Encoding]::ASCII.GetBytes("data").CopyTo($buffer, 36)
[BitConverter]::GetBytes([int]$dataLen).CopyTo($buffer, 40)
for ($i = 0; $i -lt $Samples; $i++) {
    $t = $i / $SampleRate
    $p = $i / $Samples
    $f = 600 + (1200 - 600) * $p
    $env = 1.0; if ($p -lt 0.2) { $env = $p / 0.2 } elseif ($p -gt 0.8) { $env = (1.0 - $p) / 0.2 }
    $val = [short]([Math]::Sin(2 * [Math]::PI * $f * $t) * 15000 * $env)
    $bytes = [BitConverter]::GetBytes($val)
    $buffer[44 + $i * 2] = $bytes[0]; $buffer[45 + $i * 2] = $bytes[1]
}
[System.IO.File]::WriteAllBytes("$assetsDir\mic_start.wav", $buffer)

# Stop Sound
$DurMs = 100
$Samples = [int]($SampleRate * ($DurMs / 1000))
$dataLen = $Samples * 2
$buffer = New-Object byte[] (44 + $dataLen)
[System.Text.Encoding]::ASCII.GetBytes("RIFF").CopyTo($buffer, 0)
[BitConverter]::GetBytes([int](36 + $dataLen)).CopyTo($buffer, 4)
[System.Text.Encoding]::ASCII.GetBytes("WAVE").CopyTo($buffer, 8)
[System.Text.Encoding]::ASCII.GetBytes("fmt ").CopyTo($buffer, 12)
[BitConverter]::GetBytes([int]16).CopyTo($buffer, 16)
[BitConverter]::GetBytes([short]1).CopyTo($buffer, 20)
[BitConverter]::GetBytes([short]1).CopyTo($buffer, 22)
[BitConverter]::GetBytes([int]$SampleRate).CopyTo($buffer, 24)
[BitConverter]::GetBytes([int]($SampleRate * 2)).CopyTo($buffer, 28)
[BitConverter]::GetBytes([short]2).CopyTo($buffer, 32)
[BitConverter]::GetBytes([short]16).CopyTo($buffer, 34)
[System.Text.Encoding]::ASCII.GetBytes("data").CopyTo($buffer, 36)
[BitConverter]::GetBytes([int]$dataLen).CopyTo($buffer, 40)
for ($i = 0; $i -lt $Samples; $i++) {
    $t = $i / $SampleRate
    $p = $i / $Samples
    $f = 1000 + (400 - 1000) * $p
    $env = 1.0; if ($p -lt 0.2) { $env = $p / 0.2 } elseif ($p -gt 0.8) { $env = (1.0 - $p) / 0.2 }
    $val = [short]([Math]::Sin(2 * [Math]::PI * $f * $t) * 15000 * $env)
    $bytes = [BitConverter]::GetBytes($val)
    $buffer[44 + $i * 2] = $bytes[0]; $buffer[45 + $i * 2] = $bytes[1]
}
[System.IO.File]::WriteAllBytes("$assetsDir\mic_stop.wav", $buffer)
Write-Host "Done"
