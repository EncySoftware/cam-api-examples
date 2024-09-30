@echo off
SETLOCAL

REM .Net sdk is required to build the project
echo Building the DIN4000ImportPlugin project
call dotnet build %~dp0DIN4000ImportPlugin.csproj -c Debug

pause