@echo off
cd /D %~dp0

call ..\build.cmd --Target Pack --Variant Release

pause

EXIT /B %EXIT_CODE%