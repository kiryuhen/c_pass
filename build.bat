@echo off
echo ========================================================================
echo      Сборка Password Manager - Менеджер паролей на C#
echo ========================================================================
echo.

echo 1. Создание иконки приложения...
powershell -ExecutionPolicy Bypass -File create-icon.ps1
if %ERRORLEVEL% neq 0 (
    echo ВНИМАНИЕ: Не удалось создать иконку. Сборка продолжится без иконки.
) else (
    echo Иконка успешно создана.
)
echo.

echo 2. Восстановление зависимостей...
dotnet restore
if %ERRORLEVEL% neq 0 (
    echo ОШИБКА: Не удалось восстановить зависимости.
    pause
    exit /b %ERRORLEVEL%
)
echo.

echo 3. Сборка и публикация приложения...
dotnet publish -c Release -r win-x64 -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true --self-contained true
if %ERRORLEVEL% neq 0 (
    echo ОШИБКА: Не удалось опубликовать приложение.
    pause
    exit /b %ERRORLEVEL%
)
echo.

echo 4. Создание ярлыка на рабочем столе...
set PUBLISH_PATH=%CD%\bin\Release\net6.0-windows\win-x64\publish\PasswordManager.exe
set SHORTCUT_PATH=%USERPROFILE%\Desktop\Password Manager.lnk

powershell -Command "$WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%SHORTCUT_PATH%'); $Shortcut.TargetPath = '%PUBLISH_PATH%'; $Shortcut.IconLocation = '%PUBLISH_PATH%,0'; $Shortcut.Save()"

echo.
echo ========================================================================
echo      Сборка успешно завершена!
echo ========================================================================
echo.
echo Исполняемый файл: %PUBLISH_PATH%
echo.
echo Ярлык создан на рабочем столе.
echo.
echo При первом запуске приложения вам будет предложено создать мастер-пароль,
echo который будет использоваться для защиты хранилища паролей.
echo.
echo ========================================================================
echo.

set /p choice=Запустить приложение сейчас? (y/n): 
if /i "%choice%"=="y" (
    start "" "%PUBLISH_PATH%"
)
pause