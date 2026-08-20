@echo off
cd /D %~dp0

dotnet clean "..\project\main\FeatureFinderViewerNet.csproj" --no-logo
if exist "..\project\main\obj" rmdir /s /q "..\project\main\obj"

if NOT "%nopause%"=="true" (
    pause
)
