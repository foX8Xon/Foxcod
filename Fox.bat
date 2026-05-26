@echo off
chcp 65001 >nul
title FoxCod — Управление

:: Переходим в папку батника
cd /d "%~dp0"

:: Проверка Python и engine.py
where python >nul 2>nul
if %errorlevel% neq 0 (
    echo [ОШИБКА] Python не найден!
    pause
    exit
)

if not exist "engine.py" (
    echo [ОШИБКА] engine.py не найден в этой папке!
    pause
    exit
)

:menu
cls
echo.
echo ╔════════════════════════════════════════════╗
echo ║                 FoxCod                     ║
echo ╚════════════════════════════════════════════╝
echo.
echo Команды:
echo   f  — Запустить скрипт ещё раз
echo   q  — Закрыть FoxCod
echo.
echo Подсказка всегда видна. Ожидание ввода...
echo.

:: Автозакрытие через 5 минут (300 секунд) бездействия
timeout /t 300 >nul
if %errorlevel% equ 0 (
    echo Автозакрытие из-за бездействия...
    exit
)

set "choice="
set /p "choice=Введи команду (f/q): "

if /i "%choice%"=="f" (
    echo.
    echo ================================================
    echo Запуск engine.py...
    echo ================================================
    python engine.py %1
    echo.
    echo ================================================
    echo Выполнение завершено.
    echo.
    goto menu
)

if /i "%choice%"=="q" (
    echo Закрытие FoxCod...
    exit
)

echo Неизвестная команда. Используй f или q.
goto menu
