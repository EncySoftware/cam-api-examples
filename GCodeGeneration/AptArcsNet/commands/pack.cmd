@echo off
cd /D %~dp0

call ..\.stbuild\build.cmd --Target Pack --Variant Release --no-logo

if NOT "%nopause%"=="true" (
    pause
)

EXIT /B %EXIT_CODE%
