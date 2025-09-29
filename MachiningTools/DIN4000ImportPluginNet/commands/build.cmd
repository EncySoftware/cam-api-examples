@echo off
SETLOCAL

echo Building the DIN4000ImportPluginNet project
call dotnet build %~dp0..\DIN4000ImportPluginNet.csproj

if NOT "%nopause%"=="true" (
    pause
)