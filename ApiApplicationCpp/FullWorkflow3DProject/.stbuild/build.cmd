@ECHO OFF
cd %~dp0
powershell -ExecutionPolicy ByPass -NoProfile -File "%~dp0build.ps1" %*
