@echo off
rem Double-click me. Runs start-windows.ps1 without changing your PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0start-windows.ps1" %*
echo.
pause
