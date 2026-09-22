@echo off
setlocal
call "%~dp0..\.stbuild\build.cmd" --Target Clean --Variant Debug --no-logo %*
exit /b %ERRORLEVEL%
