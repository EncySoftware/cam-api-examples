@echo off
cd /D %~dp0

call ..\build.cmd --Target Compile --Variant Debug

if NOT "%nopause%"=="true" (
    pause
)

EXIT /B %EXIT_CODE%