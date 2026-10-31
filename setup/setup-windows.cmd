@echo off
rem Double-click me. Runs setup-windows.ps1 without changing your PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup-windows.ps1" %*
echo.
pause
