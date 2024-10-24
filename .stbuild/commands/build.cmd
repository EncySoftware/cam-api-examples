@echo off
cd /D %~dp0

call ..\build.cmd --Target Compile --Variant Debug

pause

EXIT /B %EXIT_CODE%