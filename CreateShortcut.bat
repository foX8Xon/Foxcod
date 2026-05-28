@echo off
setlocal enabledelayedexpansion

set "SCRIPT_DIR=%~dp0"
set "LAUNCHER_EXE=%SCRIPT_DIR%FoxCod.Launcher\bin\Release\net10.0\FoxCod.Launcher.exe"
set "SHORTCUT_PATH=%SCRIPT_DIR%FoxCod Launcher.lnk"

if not exist "!LAUNCHER_EXE!" (
    echo Error: FoxCod Launcher executable not found
    exit /b 1
)

powershell -NoProfile -Command "^
    $shell = New-Object -ComObject WScript.Shell; ^
    $sc = $shell.CreateShortcut('%SHORTCUT_PATH%'); ^
    $sc.TargetPath = '%LAUNCHER_EXE%'; ^
    $sc.WorkingDirectory = '%SCRIPT_DIR%'; ^
    $sc.Description = 'FoxCod Script Launcher'; ^
    $sc.Save()
"

if %errorlevel% equ 0 (
    echo Shortcut created: FoxCod Launcher.lnk
) else (
    echo Failed to create shortcut
    exit /b 1
)
