@echo off
rem Double-click me. Sets up, checks, then opens Claude Code in this project.
rem Runs without changing your PowerShell execution policy.
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0setup\start-here-windows.ps1" %*
echo.
pause
