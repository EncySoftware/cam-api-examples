@echo off
cd /D %~dp0

call ..\build.cmd --Target Inject --Variant Release

if NOT "%nopause%"=="true" (
    pause
)

EXIT /B %EXIT_CODE%