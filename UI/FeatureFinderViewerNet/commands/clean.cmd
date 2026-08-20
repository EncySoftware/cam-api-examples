@echo off
cd /D %~dp0

dotnet clean "..\project\main\FeatureFinderViewerNet.csproj" --no-logo

if NOT "%nopause%"=="true" (
    pause
)
