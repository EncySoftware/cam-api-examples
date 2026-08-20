@echo off
cd /D %~dp0

dotnet build "..\project\main\FeatureFinderViewerNet.csproj" -c Debug --no-logo

if NOT "%nopause%"=="true" (
    pause
)
