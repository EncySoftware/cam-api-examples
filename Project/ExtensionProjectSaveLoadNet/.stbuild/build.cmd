@echo off
powershell -ExecutionPolicy Bypass -NoProfile -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
