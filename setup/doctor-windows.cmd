@echo off
rem Double-click me. Runs doctor-windows.ps1 without changing your PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0doctor-windows.ps1" %*
echo.
pause
