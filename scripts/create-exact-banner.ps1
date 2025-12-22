# Phase 6A.38: Create email banner matching EXACT landing page design
# Gradient: from-orange-600 (#ea580c) via-rose-800 (#9f1239) to-emerald-800 (#166534)
# Pattern: Cross/plus SVG pattern at 10% opacity

Add-Type -AssemblyName System.Drawing

$width = 650
$height = 120
$outputPath = "c:\Work\LankaConnect\scripts\email-assets\email-header-banner-v2.png"

# Create bitmap
$bitmap = New-Object System.Drawing.Bitmap($width, $height)
$graphics = [System.Drawing.Graphics]::FromImage($bitmap)
$graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$graphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$graphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Exact Tailwind colors from landing page
$orange600 = [System.Drawing.Color]::FromArgb(255, 234, 88, 12)   # #ea580c
$rose800 = [System.Drawing.Color]::FromArgb(255, 159, 18, 57)     # #9f1239
$emerald800 = [System.Drawing.Color]::FromArgb(255, 22, 101, 52)  # #166534

# Create horizontal gradient (left to right)
$rect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
$gradientBrush = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $rect,
    $orange600,
    $emerald800,
    [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal
)

# Create color blend for 3-stop gradient (orange -> rose -> emerald)
$colorBlend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
$colorBlend.Colors = @($orange600, $rose800, $emerald800)
$colorBlend.Positions = @(0.0, 0.5, 1.0)
$gradientBrush.InterpolationColors = $colorBlend

# Fill background with gradient
$graphics.FillRectangle($gradientBrush, $rect)

# Draw cross/plus pattern at 10% opacity (matching landing page SVG)
$patternPen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(25, 255, 255, 255), 2)  # 10% white
$spacing = 30

for ($x = 0; $x -lt $width; $x += $spacing) {
    for ($y = 0; $y -lt $height; $y += $spacing) {
        # Draw plus/cross shape
        $crossSize = 6
        # Horizontal line of plus
        $graphics.DrawLine($patternPen, $x - $crossSize, $y, $x + $crossSize, $y)
        # Vertical line of plus
        $graphics.DrawLine($patternPen, $x, $y - $crossSize, $x, $y + $crossSize)
    }
}

# Add "Registration Confirmed!" text
$font = New-Object System.Drawing.Font("Arial", 28, [System.Drawing.FontStyle]::Bold)
$textBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::White)
$text = "Registration Confirmed!"

# Measure text for centering
$textSize = $graphics.MeasureString($text, $font)
$textX = ($width - $textSize.Width) / 2
$textY = ($height - $textSize.Height) / 2

# Draw text shadow for depth
$shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(80, 0, 0, 0))
$graphics.DrawString($text, $font, $shadowBrush, $textX + 2, $textY + 2)

# Draw main text
$graphics.DrawString($text, $font, $textBrush, $textX, $textY)

# Add decorative sparkle icon (simple star)
$sparkleFont = New-Object System.Drawing.Font("Segoe UI Emoji", 20)
$graphics.DrawString([char]0x2728, $sparkleFont, $textBrush, $textX - 40, $textY + 5)
$graphics.DrawString([char]0x2728, $sparkleFont, $textBrush, $textX + $textSize.Width + 10, $textY + 5)

# Clean up and save
$graphics.Dispose()
$bitmap.Save($outputPath, [System.Drawing.Imaging.ImageFormat]::Png)
$bitmap.Dispose()

Write-Host "Created header banner: $outputPath"
Write-Host "Size: $((Get-Item $outputPath).Length) bytes"

# Create footer banner with logo
$footerWidth = 650
$footerHeight = 100
$footerPath = "c:\Work\LankaConnect\scripts\email-assets\email-footer-banner-v2.png"

$footerBitmap = New-Object System.Drawing.Bitmap($footerWidth, $footerHeight)
$footerGraphics = [System.Drawing.Graphics]::FromImage($footerBitmap)
$footerGraphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality
$footerGraphics.InterpolationMode = [System.Drawing.Drawing2D.InterpolationMode]::HighQualityBicubic
$footerGraphics.TextRenderingHint = [System.Drawing.Text.TextRenderingHint]::AntiAliasGridFit

# Footer gradient (same as header but darker)
$footerRect = New-Object System.Drawing.Rectangle(0, 0, $footerWidth, $footerHeight)
$footerGradient = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
    $footerRect,
    $orange600,
    $emerald800,
    [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal
)
$footerGradient.InterpolationColors = $colorBlend
$footerGraphics.FillRectangle($footerGradient, $footerRect)

# Draw cross pattern on footer too
for ($x = 0; $x -lt $footerWidth; $x += $spacing) {
    for ($y = 0; $y -lt $footerHeight; $y += $spacing) {
        $crossSize = 6
        $footerGraphics.DrawLine($patternPen, $x - $crossSize, $y, $x + $crossSize, $y)
        $footerGraphics.DrawLine($patternPen, $x, $y - $crossSize, $x, $y + $crossSize)
    }
}

# Load and draw logo
$logoPath = "c:\Work\LankaConnect\web\public\images\lankaconnect-logo.png"
if (Test-Path $logoPath) {
    $logo = [System.Drawing.Image]::FromFile($logoPath)
    $logoSize = 60
    $logoX = 30
    $logoY = ($footerHeight - $logoSize) / 2
    $footerGraphics.DrawImage($logo, $logoX, $logoY, $logoSize, $logoSize)
    $logo.Dispose()
}

# Draw "LankaConnect" text
$brandFont = New-Object System.Drawing.Font("Arial", 22, [System.Drawing.FontStyle]::Bold)
$footerGraphics.DrawString("LankaConnect", $brandFont, $textBrush, 100, 25)

# Draw tagline
$taglineFont = New-Object System.Drawing.Font("Arial", 11, [System.Drawing.FontStyle]::Regular)
$footerGraphics.DrawString("Sri Lankan Community Hub", $taglineFont, $textBrush, 100, 55)

# Draw copyright on right side
$copyrightFont = New-Object System.Drawing.Font("Arial", 10, [System.Drawing.FontStyle]::Regular)
$copyright = [char]0x00A9 + " 2025 All rights reserved"
$copyrightSize = $footerGraphics.MeasureString($copyright, $copyrightFont)
$footerGraphics.DrawString($copyright, $copyrightFont, $textBrush, $footerWidth - $copyrightSize.Width - 30, 45)

$footerGraphics.Dispose()
$footerBitmap.Save($footerPath, [System.Drawing.Imaging.ImageFormat]::Png)
$footerBitmap.Dispose()

Write-Host "Created footer banner: $footerPath"
Write-Host "Size: $((Get-Item $footerPath).Length) bytes"
