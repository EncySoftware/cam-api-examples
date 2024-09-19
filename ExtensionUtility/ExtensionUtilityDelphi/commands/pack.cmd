@echo off
cd /D %~dp0

call ..\.stbuild\build.cmd --Target Pack --Variant Release

pause

EXIT /B %EXIT_CODE%