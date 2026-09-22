@echo off
setlocal
call "%~dp0..\.stbuild\build.cmd" --Target Pack --Variant Release --no-logo %*
exit /b %ERRORLEVEL%
