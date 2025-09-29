@echo off
cd /D %~dp0

dotnet build ..\MachiningToolsCreateExampleNet.csproj

if NOT "%nopause%"=="true" (
    pause
)

EXIT /B %EXIT_CODE%