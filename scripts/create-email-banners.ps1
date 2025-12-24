# PowerShell script to create email banner images
# Requires .NET Framework with System.Drawing

Add-Type -AssemblyName System.Drawing

$outputDir = "c:\Work\LankaConnect\scripts\email-assets"
if (-not (Test-Path $outputDir)) {
    New-Item -ItemType Directory -Path $outputDir -Force
}

# Create Header Banner (650x120px)
function Create-HeaderBanner {
    $width = 650
    $height = 120

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    # Create gradient brush (orange -> rose -> emerald)
    $gradientRect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)

    # Use a linear gradient approximating the 3-color gradient
    # Orange (#ea580c) to Rose (#9f1239)
    $brush1 = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $gradientRect,
        [System.Drawing.ColorTranslator]::FromHtml("#ea580c"),
        [System.Drawing.ColorTranslator]::FromHtml("#166534"),
        [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal
    )

    # Create a color blend for 3-color gradient
    $colorBlend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
    $colorBlend.Colors = @(
        [System.Drawing.ColorTranslator]::FromHtml("#ea580c"),  # Orange
        [System.Drawing.ColorTranslator]::FromHtml("#9f1239"),  # Rose
        [System.Drawing.ColorTranslator]::FromHtml("#166534")   # Emerald
    )
    $colorBlend.Positions = @(0.0, 0.5, 1.0)
    $brush1.InterpolationColors = $colorBlend

    # Fill background with gradient
    $graphics.FillRectangle($brush1, 0, 0, $width, $height)

    # Draw cross pattern (white, 10% opacity)
    $crossColor = [System.Drawing.Color]::FromArgb(25, 255, 255, 255)  # 10% white
    $crossPen = New-Object System.Drawing.Pen($crossColor, 1)

    # Draw plus signs across the banner
    $plusSize = 8
    $spacing = 30
    for ($y = 15; $y -lt $height; $y += $spacing) {
        for ($x = 15; $x -lt $width; $x += $spacing) {
            # Vertical line of plus
            $graphics.DrawLine($crossPen, $x, ($y - $plusSize/2), $x, ($y + $plusSize/2))
            # Horizontal line of plus
            $graphics.DrawLine($crossPen, ($x - $plusSize/2), $y, ($x + $plusSize/2), $y)
        }
    }

    # Draw "Registration Confirmed!" text
    $font = New-Object System.Drawing.Font("Arial", 24, [System.Drawing.FontStyle]::Bold)
    $textBrush = [System.Drawing.Brushes]::White
    $text = "Registration Confirmed!"

    $textSize = $graphics.MeasureString($text, $font)
    $textX = ($width - $textSize.Width) / 2
    $textY = ($height - $textSize.Height) / 2

    # Add text shadow
    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(50, 0, 0, 0))
    $graphics.DrawString($text, $font, $shadowBrush, ($textX + 2), ($textY + 2))
    $graphics.DrawString($text, $font, $textBrush, $textX, $textY)

    # Save
    $headerPath = Join-Path $outputDir "email-header-banner.png"
    $bitmap.Save($headerPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $graphics.Dispose()
    $bitmap.Dispose()
    $brush1.Dispose()
    $crossPen.Dispose()
    $font.Dispose()
    $shadowBrush.Dispose()

    Write-Host "Created header banner: $headerPath"
    return $headerPath
}

# Create Footer Banner (650x180px) with logo
function Create-FooterBanner {
    $width = 650
    $height = 180

    $bitmap = New-Object System.Drawing.Bitmap($width, $height)
    $graphics = [System.Drawing.Graphics]::FromImage($bitmap)
    $graphics.SmoothingMode = [System.Drawing.Drawing2D.SmoothingMode]::HighQuality

    # Create gradient (same as header)
    $gradientRect = New-Object System.Drawing.Rectangle(0, 0, $width, $height)
    $brush1 = New-Object System.Drawing.Drawing2D.LinearGradientBrush(
        $gradientRect,
        [System.Drawing.ColorTranslator]::FromHtml("#ea580c"),
        [System.Drawing.ColorTranslator]::FromHtml("#166534"),
        [System.Drawing.Drawing2D.LinearGradientMode]::Horizontal
    )

    $colorBlend = New-Object System.Drawing.Drawing2D.ColorBlend(3)
    $colorBlend.Colors = @(
        [System.Drawing.ColorTranslator]::FromHtml("#ea580c"),
        [System.Drawing.ColorTranslator]::FromHtml("#9f1239"),
        [System.Drawing.ColorTranslator]::FromHtml("#166534")
    )
    $colorBlend.Positions = @(0.0, 0.5, 1.0)
    $brush1.InterpolationColors = $colorBlend

    $graphics.FillRectangle($brush1, 0, 0, $width, $height)

    # Draw cross pattern
    $crossColor = [System.Drawing.Color]::FromArgb(20, 255, 255, 255)
    $crossPen = New-Object System.Drawing.Pen($crossColor, 1)

    $plusSize = 8
    $spacing = 30
    for ($y = 15; $y -lt $height; $y += $spacing) {
        for ($x = 15; $x -lt $width; $x += $spacing) {
            $graphics.DrawLine($crossPen, $x, ($y - $plusSize/2), $x, ($y + $plusSize/2))
            $graphics.DrawLine($crossPen, ($x - $plusSize/2), $y, ($x + $plusSize/2), $y)
        }
    }

    # Load and draw logo (resized to 70x70)
    $logoPath = "c:\Work\LankaConnect\web\public\logos\lankaconnect-logo-transparent.png"
    if (Test-Path $logoPath) {
        $logo = [System.Drawing.Image]::FromFile($logoPath)
        $logoSize = 70
        $logoX = ($width - $logoSize) / 2
        $logoY = 20

        # Draw white circle behind logo
        $circleBrush = [System.Drawing.Brushes]::White
        $graphics.FillEllipse($circleBrush, ($logoX - 5), ($logoY - 5), ($logoSize + 10), ($logoSize + 10))

        # Draw logo
        $graphics.DrawImage($logo, $logoX, $logoY, $logoSize, $logoSize)
        $logo.Dispose()
    }

    # Draw "LankaConnect" text
    $font = New-Object System.Drawing.Font("Arial", 20, [System.Drawing.FontStyle]::Bold)
    $text = "LankaConnect"
    $textSize = $graphics.MeasureString($text, $font)
    $textX = ($width - $textSize.Width) / 2
    $textY = 100

    $shadowBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(50, 0, 0, 0))
    $graphics.DrawString($text, $font, $shadowBrush, ($textX + 1), ($textY + 1))
    $graphics.DrawString($text, $font, [System.Drawing.Brushes]::White, $textX, $textY)

    # Draw tagline
    $taglineFont = New-Object System.Drawing.Font("Arial", 12)
    $tagline = "Sri Lankan Community Hub"
    $taglineSize = $graphics.MeasureString($tagline, $taglineFont)
    $taglineX = ($width - $taglineSize.Width) / 2
    $taglineY = 125

    $taglineBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(230, 255, 255, 255))
    $graphics.DrawString($tagline, $taglineFont, $taglineBrush, $taglineX, $taglineY)

    # Draw copyright
    $copyrightFont = New-Object System.Drawing.Font("Arial", 10)
    $copyright = "© 2025 LankaConnect. All rights reserved."
    $copyrightSize = $graphics.MeasureString($copyright, $copyrightFont)
    $copyrightX = ($width - $copyrightSize.Width) / 2
    $copyrightY = 150

    $copyrightBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(200, 255, 255, 255))
    $graphics.DrawString($copyright, $copyrightFont, $copyrightBrush, $copyrightX, $copyrightY)

    # Save
    $footerPath = Join-Path $outputDir "email-footer-banner.png"
    $bitmap.Save($footerPath, [System.Drawing.Imaging.ImageFormat]::Png)

    $graphics.Dispose()
    $bitmap.Dispose()
    $brush1.Dispose()
    $crossPen.Dispose()
    $font.Dispose()
    $taglineFont.Dispose()
    $copyrightFont.Dispose()
    $shadowBrush.Dispose()
    $taglineBrush.Dispose()
    $copyrightBrush.Dispose()

    Write-Host "Created footer banner: $footerPath"
    return $footerPath
}

# Execute
Write-Host "Creating email banner images..."
$header = Create-HeaderBanner
$footer = Create-FooterBanner
Write-Host "`nDone! Banner images created in: $outputDir"
Write-Host "Header: $header"
Write-Host "Footer: $footer"
