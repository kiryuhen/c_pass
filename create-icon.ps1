# Скрипт для создания стандартной иконки приложения

# Проверяем и создаем папку Resources, если она не существует
if (-not (Test-Path -Path "Resources")) {
    New-Item -Path "Resources" -ItemType Directory
}

# Создаем стандартную иконку
Add-Type -AssemblyName System.Drawing

# Создаем временный файл .png для последующей конвертации в .ico
$tempPngPath = [System.IO.Path]::Combine([System.IO.Path]::GetTempPath(), "temp_icon.png")

# Создаем базовое изображение размером 256x256 пикселей
$bitmap = New-Object System.Drawing.Bitmap 256, 256
$g = [System.Drawing.Graphics]::FromImage($bitmap)

# Закрашиваем фон темно-синим цветом
$backgroundColor = [System.Drawing.Color]::FromArgb(0, 88, 122) # #00587A
$g.Clear($backgroundColor)

# Рисуем замок
$lockBrush = New-Object System.Drawing.SolidBrush([System.Drawing.Color]::FromArgb(205, 203, 166)) # #CDCBA6
$pen = New-Object System.Drawing.Pen([System.Drawing.Color]::FromArgb(231, 231, 222), 3) # #E7E7DE

# Рисуем корпус замка
$lockRect = New-Object System.Drawing.Rectangle(78, 100, 100, 100)
$g.FillRectangle($lockBrush, $lockRect)
$g.DrawRectangle($pen, $lockRect)

# Рисуем дужку замка
$arcRect = New-Object System.Drawing.Rectangle(88, 60, 80, 80)
$g.DrawArc($pen, $arcRect, 0, 180)

# Рисуем замочную скважину
$keyholeBrush = New-Object System.Drawing.SolidBrush($backgroundColor)
$keyholeRect = New-Object System.Drawing.Rectangle(118, 130, 20, 30)
$g.FillEllipse($keyholeBrush, $keyholeRect)
$keyholeBottomRect = New-Object System.Drawing.Rectangle(123, 150, 10, 20)
$g.FillRectangle($keyholeBrush, $keyholeBottomRect)

# Сохраняем временный .png файл
$bitmap.Save($tempPngPath, [System.Drawing.Imaging.ImageFormat]::Png)

# Освобождаем ресурсы
$g.Dispose()
$bitmap.Dispose()

# Создаем пустой .ico файл
$defaultIconPath = "default_icon.ico"

try {
    # Проверяем, доступен ли ImageMagick для конвертации
    $hasImageMagick = $null -ne (Get-Command "magick" -ErrorAction SilentlyContinue)
    
    if ($hasImageMagick) {
        # Конвертируем PNG в ICO с помощью ImageMagick
        magick convert $tempPngPath -define icon:auto-resize=256,128,64,48,32,16 $defaultIconPath
    } else {
        # Используем стандартный метод без ImageMagick (только одно разрешение)
        $bitmap = [System.Drawing.Image]::FromFile($tempPngPath)
        $memoryStream = New-Object System.IO.MemoryStream
        $bitmap.Save($memoryStream, [System.Drawing.Imaging.ImageFormat]::Icon)
        [System.IO.File]::WriteAllBytes($defaultIconPath, $memoryStream.ToArray())
        $bitmap.Dispose()
        $memoryStream.Dispose()
    }
    
    # Копируем в папку Resources
    Copy-Item -Path $defaultIconPath -Destination "Resources\password_icon.ico" -Force
    
    Write-Host "Иконка успешно создана и сохранена в Resources\password_icon.ico" -ForegroundColor Green
}
catch {
    Write-Host "Ошибка при создании иконки: $_" -ForegroundColor Red
    
    # В случае ошибки создаем пустой файл иконки
    [System.IO.File]::WriteAllBytes("Resources\password_icon.ico", (New-Object byte[] 0))
    Write-Host "Создан пустой файл иконки" -ForegroundColor Yellow
}

# Удаляем временный файл
if (Test-Path $tempPngPath) {
    Remove-Item $tempPngPath -Force
}