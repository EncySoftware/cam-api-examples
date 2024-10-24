@echo off
cd /D %~dp0

call ..\build.cmd --Target Inject --Variant Release

pause

EXIT /B %EXIT_CODE%