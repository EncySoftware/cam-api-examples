@echo off
cd /D %~dp0

dotnet build ..\MachiningToolsCreateExampleNet.csproj

pause

EXIT /B %EXIT_CODE%