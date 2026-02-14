Add-Type -AssemblyName System.Drawing
$srcPath = "C:\Users\louis\Downloads\Cosmo Whisper Logo - Edited.png"
$destPath = "c:\Users\louis\OneDrive\Documents\GitHub\CosmoWhisper-Native\master_logo.png"

$bmp = New-Object System.Drawing.Bitmap($srcPath)
$width = $bmp.Width
$height = $bmp.Height

$minX, $minY = $width, $height
$maxX, $maxY = 0, 0

# Find bounding box of non-black pixels (thresholding for safety)
for ($y = 0; $y -lt $height; $y++) {
    for ($x = 0; $x -lt $width; $x++) {
        $pixel = $bmp.GetPixel($x, $y)
        if ($pixel.R -gt 5 -or $pixel.G -gt 5 -or $pixel.B -gt 5) {
            if ($x -lt $minX) { $minX = $x }
            if ($x -gt $maxX) { $maxX = $x }
            if ($y -lt $minY) { $minY = $y }
            if ($y -gt $maxY) { $maxY = $y }
        }
    }
}

$cropWidth = $maxX - $minX + 1
$cropHeight = $maxY - $minY + 1

# Create the cropped image
$cropped = New-Object System.Drawing.Bitmap($cropWidth, $cropHeight)
$g = [System.Drawing.Graphics]::FromImage($cropped)
$g.DrawImage($bmp, 0, 0, (New-Object System.Drawing.Rectangle($minX, $minY, $cropWidth, $cropHeight)), [System.Drawing.GraphicsUnit]::Pixel)

$cropped.Save($destPath, [System.Drawing.Imaging.ImageFormat]::Png)

$g.Dispose()
$cropped.Dispose()
$bmp.Dispose()

Write-Host "Success: Image cropped to $cropWidth x $cropHeight and saved to $destPath" -ForegroundColor Green
